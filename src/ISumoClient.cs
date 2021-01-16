using SumoLib.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace SumoLib
{
    public interface ISumoClient
    {
        ISumoQuery Query(string queryText);

        ISumoQueryBuilder Builder { get; }
    }
}
