using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SumoLib.Query.Impl.Common
{
    internal static class QueryHelpers
    {
        public static string BuildRequest(string query, DateTime from, DateTime to, string timeZone="UTC", bool byReceiptTime = false, string autoParsingMode = "performance")
        {
            return JsonSerializer.Serialize(new { query, 
                from = from.ToString("yyyy-MM-ddTHH:mm:ss"), 
                to = to.ToString("yyyy-MM-ddTHH:mm:ss"), 
                timeZone, 
                byReceiptTime, 
                autoParsingMode });
        }

    }
}
