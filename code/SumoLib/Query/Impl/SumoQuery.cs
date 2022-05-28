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
using SumoLib.Query.Services;

namespace SumoLib.Query.Impl
{
    internal class SumoQuery : ISumoQuery
    {
        
        private readonly ISumoQueryExecutor executor;
        private readonly string query;
        private DateTime from;
        private DateTime to;

        internal string AuthHeader { get; }

        public string Text => query;

        public SumoQuery(string query, ISumoQueryExecutor executor)
        {
            this.executor = executor;
            this.query = query;            
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
            this.from = from.Kind != DateTimeKind.Utc ? from.ToUniversalTime() : from;
            this.to = to.Kind != DateTimeKind.Utc ? to.ToUniversalTime() : to;
            
            return this;
        }

        public async Task<IResultEnumerable<T>> RunAsync<T>(T anonymous)
        {
            return await RunAsync<T>();
        }

        public async Task<IResultEnumerable<T>> RunAsync<T>()
        {
            return await executor.RunAsync<T>(new QuerySpec(query,from,to));
        }


    }
}
