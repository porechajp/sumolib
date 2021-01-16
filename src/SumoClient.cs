using SumoLib.Config;
using SumoLib.Query;
using SumoLib.Query.Impl;
using System;
using System.Runtime.CompilerServices;

namespace SumoLib
{
    public class SumoClient
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

        internal SumoClientImpl (EndPointConfig config)
        {
            this.config = config;
        }

        public ISumoQueryBuilder Builder => new SumoQueryBuilder(config);

        public ISumoQuery Query(string queryText)
        {
            return new SumoQuery(config, queryText);
        }
    }
}
