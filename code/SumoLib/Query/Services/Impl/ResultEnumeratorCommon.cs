using SumoLib.Errors;
using SumoLib.Query.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using SumoLib.Query.Impl.Common;


namespace SumoLib.Query.Services.Impl
{
    internal abstract class ResultEnumeratorCommon
    {
        protected readonly HttpClient client;
        private readonly Uri searchJobLocation;
        private readonly int totalRecords;
        private readonly string dataType;
        private int pending;
        private int totalFetched;

        protected ResultEnumeratorCommon(HttpClient client, Uri searchJobLocation, QueryStats qs)
        {
            this.client = client;
            this.searchJobLocation = searchJobLocation;

            this.totalRecords = qs.RecordCount > 0 ? qs.RecordCount : qs.MessageCount;

            this.dataType = qs.RecordCount > 0 ? "records" : "messages";

        }

        protected JsonElement ReadNextRecordSet()
        {
            bool errorOccurred = false;
            try
            {
                var limit = this.pending > 100 ? 100 : this.pending;

                var resp = client
                    .GetAsync(new Uri($"{searchJobLocation}/{dataType}?offset={this.totalFetched}&limit={limit}")).Result;

                if (resp.IsErrorResponse(out SumoQueryException sqe))
                {
                    throw sqe;
                }

                var result = resp.Content.ReadAsStringAsync().Result;

                var jd = JsonDocument.Parse(result);
                var messagesElement = jd.RootElement.GetProperty(dataType); // .Single(o => o.Name == "messages");

                totalFetched += limit;

                //forking functionality between generic type and object[]
                return messagesElement;
            }
            catch (SumoQueryException)
            {
                errorOccurred = true;
                throw;
            }
            catch (AggregateException aex)
            {
                errorOccurred = true;
                throw new SumoQueryException($"Unhandled error : {aex.InnerException.Message}", aex.InnerException);
            }
            catch (Exception e)
            {
                errorOccurred = true;
                throw new SumoQueryException($"Unhandled error : {e.Message}", e);
            }
            finally
            {
                if(errorOccurred)
                    client.Dispose();
            }
        }

        protected bool IsNextSetOfDataNeeded(out bool exhausted)
        {
            exhausted = false;

            if (IsEnumeratorEmpty())
            {
                pending = (totalRecords - totalFetched);

                if (pending <= 0)
                {
                    exhausted = true;
                    return false;
                }

                return true;
            }

            return false;
        }

        abstract protected bool IsEnumeratorEmpty();

    }
}
