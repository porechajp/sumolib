using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SumoLib.Query.Services
{
    internal interface ISumoQueryExecutor
    {
        Task<IEnumerable<T>> RunAsync<T>();
    }
}
