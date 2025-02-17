using SumoLib.Query.Entities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SumoLib.Query
{

    public interface ISumoQueryBuilder
    {
        ISumoQueryBuilder FromSource(string source);
        ISumoQueryPipeFragmentBuilder Filter(string source);

        ISumoQuery Build();

    }

    public interface ISumoQueryPipeFragmentBuilder
    {

        ISumoQueryWhereBuilder Where(string fieldName);

        ISumoQueryPipeFragmentBuilder Parse(string parseFragment, params string[] fields);
        ISumoQueryPipeFragmentBuilder And(string fragment);

        ISumoQuery Build();
    }

    public interface ISumoQueryWhereBuilder
    {
        ISumoQueryPipeFragmentBuilder Matches(string value);
        ISumoQueryPipeFragmentBuilder NotMatches(string value);
    }

    public interface ISumoQuery
    {
        
        string Text { get; }

        ISumoQuery ForLast(TimeSpan span);

        ISumoQuery Within(DateTime from, DateTime to);

        Task<IResultEnumerable<T>> RunAsync<T>();

        Task<IResultEnumerable<T>> RunAsync<T>(T anonymous);

        Task<IResultEnumerable<Object[]>> RunAsync(IEnumerable<string> fields);         

    }

    public interface IResultEnumerable<T> : IEnumerable<T>
    {
        QueryStats Stats { get; }
    }

}
