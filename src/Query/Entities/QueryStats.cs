using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SumoLib.Query.Entities
{
    public class QueryStats
    {
        [JsonConstructor]
        public QueryStats( int messageCount, int recordCount)
        {
            this.MessageCount = messageCount;
            this.RecordCount = recordCount;
        }

        [JsonPropertyName("messageCount")]
        public int MessageCount { get; }

        [JsonPropertyName("recordCount")]
        public int RecordCount { get; }
    }
}
