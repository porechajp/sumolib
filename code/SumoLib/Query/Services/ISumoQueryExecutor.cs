using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SumoLib.Query.Services
{
    internal interface ISumoQueryExecutor
    {
        Task<IResultEnumerable<Object[]>> RunAsync(QuerySpec spec, IEnumerable<string> fields, CancellationToken cancellationToken);
        Task<IResultEnumerable<T>> RunAsync<T>(QuerySpec spec, CancellationToken cancellationToken);
    }

    internal class QuerySpec
    {
        public QuerySpec(string query, DateTime from, DateTime to)
        {
            this.Query = query;
            this.From = from;
            this.To = to;
        }

        public string Query { get; }
        public DateTime From { get; }
        public DateTime To { get; }
    }
}
