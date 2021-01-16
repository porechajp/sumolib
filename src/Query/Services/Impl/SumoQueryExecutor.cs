using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SumoLib.Query.Services.Impl
{
    internal class SumoQueryExecutor : ISumoQueryExecutor
    {
        public Task<IEnumerable<T>> RunAsync<T>()
        {
            throw new NotImplementedException();
        }
    }
}
