using System.Runtime.CompilerServices;
using System;
using System.Net.Http;


[assembly: InternalsVisibleTo("SumoLibTest")]
namespace SumoLib.Query.Services.Transport
{

    internal static class HttpClientFactory
    {
        private static HttpClient mockClient=null;

        public static HttpClient NewClient()
        {
            return mockClient ?? new HttpClient();
        }

        internal static void SetMockClient(HttpClient mockClient)
        {
            HttpClientFactory.mockClient = mockClient;
        }

        internal static void ResetMockClient()
        {
            HttpClientFactory.mockClient = null;
        }
    }
}