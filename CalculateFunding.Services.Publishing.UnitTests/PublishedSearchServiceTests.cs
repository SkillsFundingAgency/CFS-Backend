using System.Collections.Generic;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;


namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public partial class PublishedSearchServiceTests
    {
        static PublishedSearchService CreateSearchService(ISearchRepository<PublishedIndex> searchRepository = null,
           ILogger logger = null)
        {
            return new PublishedSearchService(searchRepository ?? CreateSearchRepository(), logger ?? CreateLogger());
        }

        static ISearchRepository<PublishedIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<PublishedIndex>>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static SearchModel CreateSearchModel()
        {
            return new SearchModel()
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                Top = 20,
                Filters = new Dictionary<string, string[]>
                {
                    { "periodName" , new[]{"18/19" } }
                }
            };
        }
    }
}
