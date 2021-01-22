using System;
using System.Collections.Generic;
using System.Text.Json;
using SumoLib.Query.Services;

namespace SumoLib.Query.Impl.Common
{
    internal static class QueryHelpers
    {
        public static string BuildRequest(QuerySpec querySpec, string timeZone="UTC", bool byReceiptTime = false, string autoParsingMode = "performance")
        {
            return JsonSerializer.Serialize(new 
               { 
                query = querySpec.Query, 
                from = querySpec.From.ToString("yyyy-MM-ddTHH:mm:ss"), 
                to = querySpec.To.ToString("yyyy-MM-ddTHH:mm:ss"), 
                timeZone, 
                byReceiptTime, 
                autoParsingMode });
        }

    }
}
