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

        
        private async Task<IResultEnumerable<T>> RunAsync<T>(QuerySpec querySpec, IEnumerable<string> fields)
        {
            var client = HttpClientFactory.NewClient();
            try
            {

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", AuthHeader);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.PostAsync(searchApiUri, new StringContent(QueryHelpers.BuildRequest(querySpec), Encoding.ASCII, "application/json"));
                
                
                if(resp.IsErrorResponse(out SumoQueryException sqe))
                {
                    throw sqe;
                }

                var searchJobLocation = resp.Headers.Location;

                var cookies = String.Join(";", resp.Headers.Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)).SelectMany(h => h.Value));

 
                client.DefaultRequestHeaders.Add("cookie", cookies);

                var qs = await WaitForQueryResult(client, searchJobLocation);

                return new ResultEnumerable<T>(client, searchJobLocation, qs, fields);
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

        public Task<IResultEnumerable<object[]>> RunAsync(QuerySpec spec, IEnumerable<string> fields)
        {
            if(fields==null || !fields.Any())
            {
                throw new ArgumentNullException("fields", "fields cannot be null or empty");
            }

            return this.RunAsync<Object[]>(spec, fields);
        }

        public Task<IResultEnumerable<T>> RunAsync<T>(QuerySpec spec)
        {
            return this.RunAsync<T>(spec, Enumerable.Empty<string>());
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
    }

    internal static class JsonOptions
    {
        internal static readonly JsonSerializerOptions JSON_OPTS = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true,IgnoreReadOnlyProperties=true, NumberHandling = JsonNumberHandling.AllowReadingFromString };
    }



    internal class ResultEnumerable<T> : IResultEnumerable<T>
    {
        private readonly ResultEnumerator<T> enumerator;

        public QueryStats Stats { get; }

        internal ResultEnumerable(HttpClient client, Uri searchJobLocation, QueryStats qs, IEnumerable<string> fields)
        {
            this.enumerator = new ResultEnumerator<T>(client, searchJobLocation, qs, fields);
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
        private readonly IEnumerable<string> fields;
        private IEnumerator<T> internalEnum;

        public ResultEnumerator(HttpClient client, Uri searchJobLocation, QueryStats qs, IEnumerable<string> fields)
        {
            this.client = client;
            this.searchJobLocation = searchJobLocation;
            this.fields = fields;

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
            try
            {
                var limit = pending > 100 ? 100 : pending;

                var resp = client
                    .GetAsync(new Uri($"{searchJobLocation}/{dataType}?offset={totalFetched}&limit={limit}")).Result;

                if (resp.IsErrorResponse(out SumoQueryException sqe))
                {
                    throw sqe;
                }

                var result = resp.Content.ReadAsStringAsync().Result;

                var jd = JsonDocument.Parse(result);                
                var messagesElement = jd.RootElement.GetProperty(dataType); // .Single(o => o.Name == "messages");

                totalFetched += limit;

                //forking functionality between generic type and object[]
                if (this.fields.Any())
                {

                    return ResponseDataFormatting(messagesElement);
                }
                else
                {
                    return messagesElement.EnumerateArray().Select(je =>
                        JsonSerializer.Deserialize<T>(je.GetProperty("map").GetRawText(), JsonOptions.JSON_OPTS))
                    .GetEnumerator();             
                }
            }
            catch (SumoQueryException)
            {
                client.Dispose();
                throw;
            }
            catch (AggregateException aex)
            {
                client.Dispose();
                throw new SumoQueryException($"Unhandled error : {aex.InnerException.Message}",aex.InnerException);
            }
            catch(Exception e)
            {
                client.Dispose();
                throw new SumoQueryException($"Unhandled error : {e.Message}",e);
            }
        }

        //filtering only the necessary fields
        private IEnumerator<T> ResponseDataFormatting(JsonElement messages)
        {
            foreach (var message in messages.EnumerateArray())
            {
                var map = message.GetProperty("map");

                var fieldsCount = this.fields.Count();
                var row = new object[fieldsCount + 1];

                row[0] = map.TryGetProperty("_messagetime", out JsonElement timeValue) &&                             
                              (timeValue.ValueKind == JsonValueKind.String && long.TryParse(timeValue.GetString(), out long unixMillis))
                             ? DateTimeOffset.FromUnixTimeMilliseconds(unixMillis).UtcDateTime
                             : DateTime.UtcNow;

                for (int i = 0; i < fieldsCount; i++)
                {
                    string propName = this.fields.ElementAt(i);
                    row[i + 1] = map.TryGetProperty(propName, out JsonElement value)
                        ? ConvertField(value)
                        : null;
                }

                yield return (T)(Object)row;
            }
        }

        // converting JsonValueKind types to string
        private object ConvertField(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
                return null;

            string stringValue = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false"
            };

            return stringValue;
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

