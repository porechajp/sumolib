using SumoLib.Config;
using SumoLib.Query;
using SumoLib.Query.Impl;
using SumoLib.Query.Services;
using SumoLib.Query.Services.Impl;
using System;
using System.Runtime.CompilerServices;

namespace SumoLib
{
    public static class SumoClient
    {
        
        private static EndPointConfig DefaultConfig { get; set; }

        public static void SetupDefault(EndPointConfig config) 
        {
            SumoClient.DefaultConfig = config;
        }

        public static ISumoClient New()
        {
            return new SumoClientImpl(SumoClient.DefaultConfig);
        }

        public static ISumoClient New(EndPointConfig config)
        {
            return new SumoClientImpl(config);
        }

        
    }

    internal class SumoClientImpl : ISumoClient
    {
        private readonly EndPointConfig config;
        private readonly ISumoQueryExecutor executor;

        internal SumoClientImpl (EndPointConfig config)
        {
            this.config = config;
            this.executor = new SumoQueryExecutor(config);
        }

        public ISumoQueryBuilder Builder => new SumoQueryBuilder(executor);

        public ISumoQuery Query(string queryText)
        {
            return new SumoQuery(queryText,executor);
        }
    }
}
