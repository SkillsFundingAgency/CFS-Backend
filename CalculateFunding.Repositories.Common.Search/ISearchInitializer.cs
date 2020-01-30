using CalculateFunding.Models;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Repositories.Common.Search
{
    public interface ISearchInitializer
    {
        Task Initialise<T>();
        Task InitialiseIndexer(Type indexType, SearchIndexAttribute attribute);
        Task RunIndexer<T>();
    }
}