using SumoLib.Errors;
using SumoLib.Query.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SumoLib.Query.Services.Impl
{


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

   

    internal class ResultEnumerator<T> : ResultEnumeratorCommon,IEnumerator<T>
    {
        private IEnumerator<T> internalEnum;

        public ResultEnumerator(HttpClient client, Uri searchJobLocation, QueryStats qs):base(client,searchJobLocation,qs)
        {
        }

        public T Current => internalEnum.Current;

        object IEnumerator.Current => internalEnum.Current;

        public void Dispose()
        {
            client.Dispose();
        }

        public bool MoveNext()
        {

            if (IsNextSetOfDataNeeded(out var exhausted) && !exhausted)
            {

                this.internalEnum = RequestNextSetOfData();

                if (!this.internalEnum.MoveNext())
                    return false;
            }

            return !exhausted;

        }

        private IEnumerator<T> RequestNextSetOfData()
        {
            var messagesElement = ReadNextRecordSet();

            //forking functionality between generic type and object[]
            return messagesElement.EnumerateArray().Select(je =>
                JsonSerializer.Deserialize<T>(je.GetProperty("map").GetRawText(), JsonOptions.JSON_OPTS))
            .GetEnumerator();
        }

       

        protected override bool IsEnumeratorEmpty()
        {
            return this.internalEnum == null || !this.internalEnum.MoveNext();
        }


        public void Reset()
        {
            throw new NotImplementedException();
        }

    }
}
