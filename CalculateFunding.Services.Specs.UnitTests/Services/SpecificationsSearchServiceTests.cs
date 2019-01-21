using System.Collections.Generic;
using CalculateFunding.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    [TestClass]
    public partial class SpecificationsSearchServiceTests
    {
        static SpecificationsSearchService CreateSearchService(ISearchRepository<SpecificationIndex> searchRepository = null,
            ILogger logger = null)
        {
            return new SpecificationsSearchService(searchRepository ?? CreateSearchRepository(), logger ?? CreateLogger());
        }

        static ISearchRepository<SpecificationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<SpecificationIndex>>();
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
