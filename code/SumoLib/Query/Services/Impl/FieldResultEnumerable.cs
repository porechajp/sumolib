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
    internal class FieldsResultEnumerable : IResultEnumerable<object[]>
    {

        private readonly FieldResultEnumerator enumerator;

        public QueryStats Stats { get; }

        internal FieldsResultEnumerable(HttpClient client, Uri searchJobLocation, QueryStats qs, IEnumerable<string> fields)
        {
            this.enumerator = new FieldResultEnumerator(client, searchJobLocation, qs, fields);
            this.Stats = qs;
        }


        public IEnumerator<object[]> GetEnumerator()
        {
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return enumerator;
        }
    }


    internal class FieldResultEnumerator : ResultEnumeratorCommon, IEnumerator<object[]>
    {
        private readonly IEnumerable<string> fields;

        private IEnumerator<object[]> internalEnum;

        public FieldResultEnumerator(HttpClient client, Uri searchJobLocation, QueryStats qs, IEnumerable<string> fields) : base(client, searchJobLocation, qs)
        {
            this.fields = fields;
        }

        public object[] Current => internalEnum.Current;

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

        private IEnumerator<object[]> RequestNextSetOfData()
        {
            var messagesElement = ReadNextRecordSet();


            return ResponseDataFormatting(messagesElement);

        }

        private IEnumerator<object[]> ResponseDataFormatting(JsonElement messages)
        {
            var fieldsCount = this.fields.Count();

            foreach (var message in messages.EnumerateArray())
            {
                var map = message.GetProperty("map");

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

                yield return row;
            }
        }

        // converting JsonValueKind types to string
        private string ConvertField(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
                return null;

            string stringValue = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => value.ToString(),
            };

            return stringValue;
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
