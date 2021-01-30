using System.Runtime.CompilerServices;
using System;
using System.Net.Http;
using System.Threading;


[assembly: InternalsVisibleTo("SumoLibTest")]
namespace SumoLib.Query.Services.Transport
{

    internal static class HttpClientFactory
    {
        private  static readonly ThreadLocal<HttpClient> mockClient=new ThreadLocal<HttpClient>();

        public static HttpClient NewClient()
        {
            return mockClient.Value ?? new HttpClient();
        }

        internal static IDisposable SetMockClient(HttpClient mockClient)
        {
            HttpClientFactory.mockClient.Value = mockClient;

            return new TLMockDisposable();

        }

        private struct TLMockDisposable : IDisposable
        {
            public void Dispose()
            {
                HttpClientFactory.mockClient.Value = null;
            }
        }
    }
}