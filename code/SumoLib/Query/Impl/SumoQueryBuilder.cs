using SumoLib.Config;
using SumoLib.Query.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SumoLib.Query.Impl
{
    internal class SumoQueryBuilder : ISumoQueryBuilder
    {
        private readonly ISumoQueryExecutor executor;

        private readonly StringBuilder query;

        public SumoQueryBuilder(ISumoQueryExecutor executor)
        {
            this.executor = executor;
            this.query = new StringBuilder(256);
        }

        public ISumoQuery Build()
        {
            return new SumoQuery(query.ToString(),executor);
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
            

            return new SumoQueryPipeFragmentBuilder(query,executor);
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
        private readonly ISumoQueryExecutor executor;

        internal SumoQueryPipeFragmentBuilder(StringBuilder query, ISumoQueryExecutor executor)
        {
            this.query = query;
            this.executor = executor;
        }

        public ISumoQuery Build()
        {
            return new SumoQuery(query.ToString(),executor);
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
