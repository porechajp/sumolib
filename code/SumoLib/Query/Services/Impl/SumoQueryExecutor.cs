using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SumoLib.Config;
using SumoLib.Errors;
using SumoLib.Query.Entities;
using SumoLib.Query.Impl.Common;
using SumoLib.Query.Services.Transport;

namespace SumoLib.Query.Services.Impl
{
    internal class SumoQueryExecutor : ISumoQueryExecutor
    {
        private readonly Uri searchApiUri;
        private readonly string AuthHeader;

        public SumoQueryExecutor(EndPointConfig config)
        {
            this.searchApiUri = new Uri(config.ApiUri, "v1/search/jobs");
            this.AuthHeader = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{config.AccessId}:{config.AccessKey}"));
        }



        public async Task<IResultEnumerable<T>> RunAsync<T>(QuerySpec querySpec)
        {
            var client = HttpClientFactory.NewClient();
            try
            {

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", AuthHeader);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.PostAsync(searchApiUri, new StringContent(QueryHelpers.BuildRequest(querySpec), Encoding.ASCII, "application/json"));
                
                if(IsErrorResponse(resp, out SumoQueryException sqe))
                {
                    throw sqe;
                }

                var searchJobLocation = resp.Headers.Location;

                var cookies = String.Join(";", resp.Headers.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).SelectMany(h => h.Value));


                client.DefaultRequestHeaders.Add("cookie", cookies);

                var qs = await WaitForQueryResult(client, searchJobLocation);

                return new ResultEnumerable<T>(client, searchJobLocation, qs);
            }
            catch (SumoQueryException)            
            {
                throw;
            }
            catch(Exception e)
            {
                client.Dispose();
                throw new SumoQueryException($"Unhandled error : {e.Message}",e);
            }
             
        }

        private bool IsErrorResponse(HttpResponseMessage resp, out SumoQueryException sqe)
        {
            if (resp.StatusCode == System.Net.HttpStatusCode.OK || resp.StatusCode == System.Net.HttpStatusCode.Accepted || resp.StatusCode == System.Net.HttpStatusCode.Redirect )
            {
                sqe = null;
                return false;
            }

            var result = resp.Content.ReadAsStringAsync().Result;
            var respJson = JsonDocument.Parse(string.IsNullOrEmpty(result) ? "{}":result);

            if (respJson.RootElement.TryGetProperty("code", out JsonElement codeElement))
            {
                sqe = new SumoQueryException(codeElement.GetString(), respJson.RootElement.GetProperty("message").GetString());                    
            }
            else if (respJson.RootElement.TryGetProperty("message", out JsonElement msgElement))
            {                    
                sqe= new SumoQueryException(msgElement.GetString());
            }
            else
            {
                sqe = new SumoQueryException($"Response status {(int)resp.StatusCode} - {resp.StatusCode.ToString()}");
            }
            
            return true;            
            
        }

        private async Task<QueryStats> WaitForQueryResult(HttpClient client, Uri searchJobLocation)
        {
            JsonDocument jd = null;
            string resultCode = null;
            do
            {


                await Task.Delay(TimeSpan.FromSeconds(1));

                var resp = await client.GetAsync(searchJobLocation);

                if(IsErrorResponse(resp, out SumoQueryException sqe))
                {
                    throw sqe;
                }

                var result = await resp.Content.ReadAsStringAsync();

                jd = JsonDocument.Parse(result);

                resultCode = jd.RootElement.GetProperty("state").GetString();

                if(resultCode == "FORCE PAUSED")
                {
                    throw new Exception("Query posed");
                }
                if(resultCode == "CANCELLED")
                {
                    throw new Exception("Cancelled");
                }


            } while ( resultCode != "DONE GATHERING RESULTS");

            return JsonSerializer.Deserialize<QueryStats>(jd.RootElement.GetRawText());

        }
    }

    internal static class JsonOptions
    {
        internal static readonly JsonSerializerOptions JSON_OPTS = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true,IgnoreReadOnlyProperties=true, NumberHandling = JsonNumberHandling.AllowReadingFromString };
    }


    internal class ResultEnumerable<T> : IResultEnumerable<T>
    {
        private readonly ResultEnumerator<T> enumerator;

        public QueryStats Stats { get; }

        internal ResultEnumerable(HttpClient client, Uri searchJobLocation, QueryStats qs)
        {
            this.enumerator = new ResultEnumerator<T>(client, searchJobLocation, qs);
            this.Stats = qs;
        }
            


        public IEnumerator<T> GetEnumerator()
        {
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return enumerator;
        }
    }

    internal class ResultEnumerator<T> : IEnumerator<T>
    {
        private readonly HttpClient client;
        private readonly Uri searchJobLocation;
        private readonly int totalRecords;
        private readonly string dataType;
        private int totalFetched;
        private int pending;
        private IEnumerator<T> internalEnum;

        public ResultEnumerator(HttpClient client, Uri searchJobLocation, QueryStats qs)
        {
            this.client = client;
            this.searchJobLocation = searchJobLocation;
            

            this.totalRecords = qs.RecordCount > 0 ? qs.RecordCount : qs.MessageCount;

            this.dataType = qs.RecordCount > 0 ? "records" : "messages";
        }

        public T Current => internalEnum.Current;

        object IEnumerator.Current => internalEnum.Current;

        public void Dispose()
        {
            client.Dispose();
        }

        public bool MoveNext()
        {

            if (IsNextSetOfDataNeeded())
            {
                this.pending = (totalRecords - totalFetched);

                if (this.pending <= 0)
                    return false;
                
                this.internalEnum = RequestNextSetOfData();

                if (!this.internalEnum.MoveNext())
                    return false;
            }

            return true;

        }

        private IEnumerator<T> RequestNextSetOfData()
        {
            var limit = pending > 100 ? 100 : pending;

            var result = client.GetAsync(new Uri($"{searchJobLocation}/{dataType}?offset={totalFetched}&limit={limit}"))
                .ContinueWith(t => t.Result.Content.ReadAsStringAsync());

            var jd = JsonDocument.Parse(result.Result.Result);

            var messagesElement = jd.RootElement.GetProperty(dataType);// .Single(o => o.Name == "messages");

            totalFetched += limit;

            return messagesElement.EnumerateArray().Select(je => JsonSerializer.Deserialize<T>(je.GetProperty("map").GetRawText(), JsonOptions.JSON_OPTS)).GetEnumerator();
        }

        private bool IsNextSetOfDataNeeded()
        {
            return this.internalEnum == null || !this.internalEnum.MoveNext();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

}

