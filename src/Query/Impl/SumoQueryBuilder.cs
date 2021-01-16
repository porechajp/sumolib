using SumoLib.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace SumoLib.Query.Impl
{
    internal class SumoQueryBuilder : ISumoQueryBuilder
    {
        private readonly EndPointConfig config;
        private readonly StringBuilder query;

        public SumoQueryBuilder(EndPointConfig config)
        {
            this.config = config;
            this.query = new StringBuilder(256);
        }

        public ISumoQuery Build()
        {
            return new SumoQuery(config, query.ToString());
        }

        public ISumoQueryPipeFragmentBuilder Filter(string filter)
        {
            if (filter.IndexOf(' ') >= 0)
            {
                this.query.Append('"').Append(filter).Append('"');
            }
            else
            {
                this.query.Append(filter);
            }
            

            return new SumoQueryPipeFragmentBuilder(query,config);
        }

        public ISumoQueryBuilder FromSource(string source)
        {            
            this.query.Append(source).Append(' ');
            return this;
        }
    }

    internal class SumoQueryPipeFragmentBuilder : ISumoQueryPipeFragmentBuilder
    {
        private readonly StringBuilder query;
        private readonly EndPointConfig config;

        internal SumoQueryPipeFragmentBuilder(StringBuilder query, EndPointConfig config)
        {
            this.query = query;
            this.config = config;
        }

        public ISumoQuery Build()
        {
            return new SumoQuery(config, query.ToString());
        }

        public ISumoQueryPipeFragmentBuilder And(string fragment)
        {
            this.query.Append('|').Append(fragment);
            return this;
        }

        public ISumoQueryPipeFragmentBuilder Parse(string parseFragment, params string[] fields)
        {
            this.query.Append('|').Append("parse ").Append('"').Append(parseFragment).Append("\" as ").Append(string.Join(",", fields));
            return this;
        }

        public ISumoQueryWhereBuilder Where(string fieldName)
        {
            return new SumoQueryWhereBuilder(this, fieldName);
        }

        internal class SumoQueryWhereBuilder : ISumoQueryWhereBuilder
        {
            private readonly SumoQueryPipeFragmentBuilder parent;
            private readonly string fieldName;

            public SumoQueryWhereBuilder(SumoQueryPipeFragmentBuilder parent, string fieldName)
            {
                this.parent = parent;
                this.fieldName = fieldName;
            }

            public ISumoQueryPipeFragmentBuilder Matches(string value)
            {
                parent.query.Append("| where ").Append(fieldName).Append(" matches ").Append('"').Append(value).Append('"');
                return parent;

            }

            public ISumoQueryPipeFragmentBuilder NotMatches(string value)
            {
                parent.query.Append("| where !(").Append(fieldName).Append(" matches ").Append('"').Append(value).Append("\")");
                return parent;
            }
        }

    }

}
