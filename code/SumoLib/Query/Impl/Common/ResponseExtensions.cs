using System.Net.Http;
using System.Text.Json;
using SumoLib.Errors;

namespace SumoLib.Query.Impl.Common
{
    internal static class ResponseExtensions
    {
        public static bool IsErrorResponse(this HttpResponseMessage resp, out SumoQueryException sqe)
        {
            if (resp.StatusCode == System.Net.HttpStatusCode.OK || resp.StatusCode == System.Net.HttpStatusCode.Accepted || resp.StatusCode == System.Net.HttpStatusCode.Redirect )
            {
                sqe = null;
                return false;
            }

            var result = resp.Content.ReadAsStringAsync().Result;
            var respJson = JsonDocument.Parse(string.IsNullOrEmpty(result) ? "{}":result);

            if (respJson.RootElement.TryGetProperty("code", out JsonElement codeElement))
            {
                sqe = new SumoQueryException(codeElement.GetString(), respJson.RootElement.GetProperty("message").GetString());                    
            }
            else if (respJson.RootElement.TryGetProperty("message", out JsonElement msgElement))
            {                    
                sqe= new SumoQueryException(msgElement.GetString());
            }
            else
            {
                sqe = new SumoQueryException($"Response status {(int)resp.StatusCode} - {resp.StatusCode.ToString()}");
            }
            
            return true;            
            
        }
    }
}