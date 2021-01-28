namespace SumoLibTest.TestData
{


    internal static class MockedResponses
    {
        
        internal const string BadEndPoint = @"{""message"":""Not Found"",""status"":""404""}";
        internal const string NoAuthHeaderResponse = @"{""message"":""Full authentication is required to access this resource"",""status"":""401""}";

        internal const string BadAuthHeaderResponse = @"{""message"":""Full authentication is required to access this resource"",""status"":""401"", ""code"":""unauthorized""}";

        internal const string InvalidJobResponse =  @"{""message"":""Job ID is invalid."",""status"":""404"", ""code"":""jobid.invalid""}";
    }

}