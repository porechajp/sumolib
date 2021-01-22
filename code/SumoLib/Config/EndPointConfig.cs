using System;
using System.Collections.Generic;
using System.Text;

namespace SumoLib.Config
{
    public class EndPointConfig
    {

        private EndPointConfig(string accessId, string accessKey)
        {
            this.AccessId = accessId;
            this.AccessKey = accessKey;
        }

        public EndPointConfig(string accessId, string accessKey, string apiUri)
        {
            this.AccessId = accessId;
            this.AccessKey = accessKey;
            this.ApiUri = new Uri(apiUri.EndsWith("/") ? apiUri : $"{apiUri}/");
        }

        public string AccessId { get; private set; }
        public string AccessKey { get; private set; }
        public Uri ApiUri { get; private set; }

        
        public EndPointConfig CopyWith(string accessId, string accessKey)
        {
            return new EndPointConfig(accessId, accessKey) { ApiUri = this.ApiUri };
        }

    }
}
