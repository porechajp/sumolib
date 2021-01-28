using System;
using Xunit;
using SumoLib;

namespace SumoLibTest
{
    public class QueryBuilderTests
    {
        [Fact]
        public void SimpleQueryBuilderTest()
        {
            
            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk")).Builder;

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .And("fields uid,fname")
            .Build().Text;

            Assert.Equal(@"(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch) Authentication|fields uid,fname",query);

        }

        [Fact]
        public void WhereClauseBuilderTest()
        {
            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk")).Builder;

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .Where("ts").Matches("Monday")
            .And("fields uid,fname")
            .Build().Text;

            Assert.Equal(@"(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch) Authentication| where ts matches ""Monday""|fields uid,fname",query);
        }

        [Fact]
        public void ParseClauseBuilderTest()
        {
            var bldr = SumoClient.New(new SumoLib.Config.EndPointConfig("mock","mock","https://mock.mk")).Builder;

            var query = bldr.FromSource("(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch)")
            .Filter("Authentication")
            .Parse("[*:*]","uid","fname")
            .And("fields uid,fname")
            .Build().Text;

            Assert.Equal(@"(_sourceCategory=env*/webapp OR _sourceCategory=env*/batch) Authentication|parse ""[*:*]"" as uid,fname|fields uid,fname",query);

        }


    }
}
