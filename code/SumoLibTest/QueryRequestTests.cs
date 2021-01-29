using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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
        public async void BadJobIdResponse()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;


            var mockRedirectRes = new HttpResponseMessage(HttpStatusCode.Redirect);
            mockRedirectRes.Headers.Add("Location","https://mock.mk/api/v1/search/jobs/29323");
            
            mockHttpHandler.When(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs")
                            .Respond(req => mockRedirectRes);
                           

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

        [Fact]
        public async void WaitIterationTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            var mockRedirectRes = new HttpResponseMessage(HttpStatusCode.Redirect);
            mockRedirectRes.Headers.Add("Location","https://mock.mk/api/v1/search/jobs/29323");
            
            mockHttpHandler.Expect(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs")
                            .Respond(req => mockRedirectRes);


            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "NOT STARTED"}));
            
            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "GATHERING RESULTS"}));

            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "DONE GATHERING RESULTS",messageCount=100}));
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .And("fields uid,fname")
            .Build();

            var data = await query.ForLast(TimeSpan.FromDays(1)).RunAsync(new {uid = "", fname = ""});

            mockHttpHandler.VerifyNoOutstandingExpectation();
            Assert.Equal(100,data.Stats.MessageCount);
                       

        }
        
        [Fact]
        public async void WaitIterationFailTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            var mockRedirectRes = new HttpResponseMessage(HttpStatusCode.Redirect);
            mockRedirectRes.Headers.Add("Location","https://mock.mk/api/v1/search/jobs/29323");
            
            mockHttpHandler.Expect(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs")
                .Respond(req => mockRedirectRes);


            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "NOT STARTED"}));
            
            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "GATHERING RESULTS"}));

            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Throw(new System.IO.IOException("Read Timeout"));
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
                .Filter("Authentication")
                .And("fields uid,fname")
                .Build();

            var sqe = await Assert.ThrowsAsync<SumoQueryException>(()=>  query.ForLast(TimeSpan.FromDays(1)).RunAsync(new {uid = "", fname = ""}));

            mockHttpHandler.VerifyNoOutstandingExpectation();

            Assert.IsType<IOException>(sqe.InnerException);
            
            Assert.Equal("Read Timeout",sqe.InnerException.Message);


        }
        
        [Fact]
        public async void WaitIterationInternalServerErrorTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            var mockRedirectRes = new HttpResponseMessage(HttpStatusCode.Redirect);
            mockRedirectRes.Headers.Add("Location","https://mock.mk/api/v1/search/jobs/29323");
            
            mockHttpHandler.Expect(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs")
                .Respond(req => mockRedirectRes);


            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "NOT STARTED"}));
            
            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "GATHERING RESULTS"}));

            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.InternalServerError);
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
                .Filter("Authentication")
                .And("fields uid,fname")
                .Build();

            var sqe = await Assert.ThrowsAsync<SumoQueryException>(()=>  query.ForLast(TimeSpan.FromDays(1)).RunAsync(new {uid = "", fname = ""}));

            mockHttpHandler.VerifyNoOutstandingExpectation();

            Assert.Equal("Response status 500 - InternalServerError", sqe.Message);

        }
        
        [Fact]
        public async void ResultsIterationTest()
        {
            var mockHttpHandler = new MockHttpMessageHandler();

            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk/api")).Builder;

            var mockRedirectRes = new HttpResponseMessage(HttpStatusCode.Redirect);
            mockRedirectRes.Headers.Add("Location","https://mock.mk/api/v1/search/jobs/29323");
            
            mockHttpHandler.Expect(HttpMethod.Post, "https://mock.mk/api/v1/search/jobs")
                .Respond(req => mockRedirectRes);


            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "NOT STARTED"}));
            
            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "GATHERING RESULTS"}));

            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {state = "DONE GATHERING RESULTS",messageCount=10}));
            
            mockHttpHandler.Expect(HttpMethod.Get, "https://mock.mk/api/v1/search/jobs/29323/messages?offset=0&limit=10")
                .Respond(HttpStatusCode.OK, JsonContent.Create(new {messages = Enumerable.Range(0,10).Select(i=>new {map=new {uid=i,fname=$"name_{i}"} }) }));
            
            HttpClientFactory.SetMockClient(mockHttpHandler.ToHttpClient());

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
                .Filter("Authentication")
                .And("fields uid,fname")
                .Build();

            var data = await query.ForLast(TimeSpan.FromDays(1)).RunAsync(new {uid = 0, fname = ""});

            Assert.Equal(10,data.Stats.MessageCount);

            var first = data.First();
            var last = data.Last();
            Assert.Equal(0,first.uid);
            Assert.Equal(9,last.uid);
            Assert.Equal("name_0",first.fname);
            Assert.Equal("name_9",last.fname);

            

        }

    }
    
}