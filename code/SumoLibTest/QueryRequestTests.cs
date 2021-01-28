using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using RichardSzalay.MockHttp;
using SumoLib;
using SumoLib.Errors;
using SumoLib.Query.Services.Transport;
using SumoLibTest.TestData;
using Xunit;

namespace SumoLibTest
{

    public class QueryRequestTests
    {

        [Fact]
        public async void ErrorCodeTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            mockHttpHandler.When(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs").Respond(HttpStatusCode.Unauthorized,new StringContent(MockedResponses.BadAuthHeaderResponse));
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .And("fields uid,fname")
            .Build();

            var sqe = await Assert.ThrowsAsync<SumoQueryException>(()=>query.ForLast(TimeSpan.FromDays(1)).RunAsync(new { uid="",fname="" }));

            Assert.Equal("unauthorized",sqe.ErrorCode);

        }

        [Fact]
        public async void ErrorMessageTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            mockHttpHandler.When(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs").Respond(HttpStatusCode.Unauthorized,new StringContent(MockedResponses.NoAuthHeaderResponse));
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .And("fields uid,fname")
            .Build();

            var sqe = await Assert.ThrowsAsync<SumoQueryException>(()=>query.ForLast(TimeSpan.FromDays(1)).RunAsync(new { uid="",fname="" }));

            Assert.True(sqe.Message.Contains("Full authentication is required to access this resource"));

        }

         [Fact]
        public async void BadURLTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            mockHttpHandler.When(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs").Throw(new System.IO.IOException("Remote address not reachable."));
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .And("fields uid,fname")
            .Build();

            var sqe = await Assert.ThrowsAsync<SumoQueryException>(()=>query.ForLast(TimeSpan.FromDays(1)).RunAsync(new { uid="",fname="" }));

            Assert.Equal("Remote address not reachable.",sqe.InnerException.Message);

        }

        
        [Fact]
        public async void BadRedirectResponse()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;


            var mockRedirectRes = new HttpResponseMessage(HttpStatusCode.Redirect);
            mockRedirectRes.Headers.Add("Location","https://mock.mk/api/v1/search/jobs/29323");
            
            mockHttpHandler.When(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs")
                            .Respond(req => mockRedirectRes);
                           //.Respond(HttpStatusCode.Unauthorized,new StringContent("{}")) ;

                           //.WithHeaders("Location","https://mock.mk/api/vi/search/jobs/29323")

            mockHttpHandler.When(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                           .Respond(HttpStatusCode.NotFound,new StringContent(MockedResponses.InvalidJobResponse));

            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .And("fields uid,fname")
            .Build();

            var sqe = await Assert.ThrowsAsync<SumoQueryException>(()=>query.ForLast(TimeSpan.FromDays(1)).RunAsync(new { uid="",fname="" }));

            Assert.Equal("jobid.invalid",sqe.ErrorCode);           

        }

    }
    
}