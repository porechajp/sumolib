using SumoLib.Query.Impl.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using SumoLib.Query.Entities;
using System.IO;
using System.Collections;
using SumoLib.Config;

namespace SumoLib.Query.Impl
{
    internal class SumoQuery : ISumoQuery
    {
        
        private readonly Uri searchApiUri;
        private readonly string query;
        private DateTime from;
        private DateTime to;

        internal string AuthHeader { get; }

        public string Text => query;

        public SumoQuery(EndPointConfig config, string query)
        {
            this.searchApiUri = new Uri(config.ApiUri, "v1/search/jobs");
            this.query = query;            
            this.AuthHeader = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{config.AccessId}:{config.AccessKey}"));

            this.to = DateTime.UtcNow;
            this.from = to.Subtract(TimeSpan.FromMinutes(15));
        }

        public ISumoQuery ForLast(TimeSpan span)
        {
            this.to = DateTime.UtcNow;
            this.from = to.Subtract(span);
            return this;
        }

        public ISumoQuery Within(DateTime from, DateTime to)
        {
            if(from.Kind != DateTimeKind.Utc)
            {
                this.from = from.ToUniversalTime();
            }
            if (to.Kind != DateTimeKind.Utc)
            {
                this.to = to.ToUniversalTime();
            }
            return this;
        }

        public async Task<IResultEnumerable<T>> RunAsync<T>(T anonymous)
        {
            return await RunAsync<T>();
        }

        public async Task<IResultEnumerable<T>> RunAsync<T>()
        {
            var client = new HttpClient();
            try
            {

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", AuthHeader);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.PostAsync(searchApiUri, new StringContent(QueryHelpers.BuildRequest(query, from, to), Encoding.ASCII, "application/json"));

                if (resp.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    throw new Exception($"Some issue : {resp.StatusCode} Data : {resp.Content.ReadAsStringAsync().Result}");
                }

                var searchJobLocation = resp.Headers.Location;

                var cookies = String.Join(";", resp.Headers.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).SelectMany(h => h.Value));


                client.DefaultRequestHeaders.Add("cookie", cookies);

                var qs = await WaitForQueryResult(client, searchJobLocation);

                return new ResultEnumerable<T>(client, searchJobLocation, qs);
            }
            catch(Exception)
            {
                client.Dispose();
                throw;
            }
             
        }


        private async Task<QueryStats> WaitForQueryResult(HttpClient client, Uri searchJobLocation)
        {
            JsonDocument jd = null;
            string resultCode = null;
            do
            {


                await Task.Delay(TimeSpan.FromSeconds(1));

                var result = await await client.GetAsync(searchJobLocation).ContinueWith(t => t.Result.Content.ReadAsStringAsync());

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
