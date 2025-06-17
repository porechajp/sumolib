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


        private async Task<SumoRequest> InitiateSumoQueryRequest(QuerySpec querySpec)
        {
            var sumoRequest = new SumoRequest();

            var client = HttpClientFactory.NewClient();

            sumoRequest.Client = client;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", AuthHeader);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var resp = await client.PostAsync(searchApiUri, new StringContent(QueryHelpers.BuildRequest(querySpec), Encoding.ASCII, "application/json"));


            if (resp.IsErrorResponse(out SumoQueryException sqe))
            {
                throw sqe;
            }

            var searchJobLocation = resp.Headers.Location;

            sumoRequest.SearchJobLocation= searchJobLocation;

            var cookies = String.Join(";", resp.Headers.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).SelectMany(h => h.Value));


            client.DefaultRequestHeaders.Add("cookie", cookies);

            var qs = await WaitForQueryResult(client, searchJobLocation);

            sumoRequest.QuerySts = qs;

            return sumoRequest;
        }

        private async Task<IResultEnumerable<object[]>> RunAsyncInternal(QuerySpec querySpec, IEnumerable<string> fields)
        {

            SumoRequest sumoRequest = null;
            try
            {
                sumoRequest = await InitiateSumoQueryRequest(querySpec);
                return new FieldsResultEnumerable(sumoRequest.Client, sumoRequest.SearchJobLocation, sumoRequest.QuerySts, fields);
            }
            catch (SumoQueryException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (sumoRequest?.Client != null)
                    sumoRequest.Client.Dispose();

                throw new SumoQueryException($"Unhandled error : {e.Message}", e);
            }

        }


        private async Task<IResultEnumerable<T>> RunAsyncInternal<T>(QuerySpec querySpec)
        {

            SumoRequest? sumoRequest=null;
            try
            {
                sumoRequest = await InitiateSumoQueryRequest(querySpec);
                return new ResultEnumerable<T>(sumoRequest.Client, sumoRequest.SearchJobLocation, sumoRequest.QuerySts);
            }
            catch (SumoQueryException)            
            {
                throw;
            }
            catch(Exception e)
            {
                if (sumoRequest?.Client != null)
                    sumoRequest.Client.Dispose();

                throw new SumoQueryException($"Unhandled error : {e.Message}",e);
            }
             
        }

        public Task<IResultEnumerable<object[]>> RunAsync(QuerySpec spec, IEnumerable<string> fields)
        {
            if(fields==null || !fields.Any())
            {
                throw new ArgumentNullException("fields", "fields cannot be null or empty");
            }

            return this.RunAsyncInternal(spec, fields);
        }

        public Task<IResultEnumerable<T>> RunAsync<T>(QuerySpec spec)
        {
            return this.RunAsyncInternal<T>(spec);
        }

        private async Task<QueryStats> WaitForQueryResult(HttpClient client, Uri searchJobLocation)
        {
            JsonDocument jd = null;
            string resultCode = null;
            do
            {


                await Task.Delay(TimeSpan.FromSeconds(1));

                var resp = await client.GetAsync(searchJobLocation);

                if(resp.IsErrorResponse(out SumoQueryException sqe))
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


        private class SumoRequest
        {
            internal HttpClient Client { get; set; }
            internal Uri SearchJobLocation { get; set; }

            internal QueryStats QuerySts { get; set; }
        }

    }

    internal static class JsonOptions
    {
        internal static readonly JsonSerializerOptions JSON_OPTS = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true,IgnoreReadOnlyProperties=true, NumberHandling = JsonNumberHandling.AllowReadingFromString };
    }   

}

