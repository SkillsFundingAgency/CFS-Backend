﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class ProviderCalculationResultsSearchServiceTests
    {
        private const int ProviderCalculationResultsFacetCount = 11;

        [TestMethod]
        public async Task SearchCalculationProviderResults_GivenNullModel_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger: logger);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("A null or invalid search model was provided for searching calculation provider results"));
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel();
            ILogger logger = CreateLogger();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculation provider results");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenTopIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };
            ILogger logger = CreateLogger();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching calculation provider results");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        [DataRow(true, "calculationId")]
        [DataRow(false, "fundingLineId")]
        public async Task SearchCalculationProviderResults_GivenSearchRequestFails_ThenBadRequestReturned(
            bool useCalculationId, string idFilterName)
        {
            //Arrange
            SearchModel model = new SearchModel()
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                IncludeFacets = false,
                Top = 50,
                Filters = new Dictionary<string, string[]>()
                {
                    { idFilterName, new string []{ "test" } }
                }
            };
            ILogger logger = CreateLogger();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model, useCalculationId);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Any<FailedToQuerySearchException>(), "Failed to query search with term: SearchTermTest");

            result
                 .Should()
                 .BeOfType<StatusCodeResult>()
                 .Which.StatusCode.Should().Be(500);
        }

        [TestMethod]
        [DataRow(true, "calculationId")]
        [DataRow(false, "fundingLineId")]
        public async Task SearchTestScenarioResults_GivenValidModelAndIncludesGettingFacets_CallsSearchCorrectNumberOfTimes(
            bool useCalculationId, string idFilterName)
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { idFilterName, new string []{ "test" } }
                }
            };
            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>(), Arg.Any<bool>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model, useCalculationId);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(ProviderCalculationResultsFacetCount + 1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.SearchFields.Any(f => f == "providerName")));

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.SearchFields.Any(f => f == "providerName") && c.SearchFields.Any(f => f == "ukPrn") && c.SearchFields.Any(f => f == "urn") && c.SearchFields.Any(f => f == "establishmentNumber")));
        }

        [TestMethod]
        [DataRow(true, "calculationId", "calculationException")]
        [DataRow(false, "fundingLineId", "fundingLineException")]
        public async Task SearchTestScenarioResults_GivenValidModelAndIncludesGettingFacets_CallsSearchOnceForErrorCount(
            bool useCalculationId, string idFilterName, string exceptionFilterName)
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { idFilterName, new string []{ "test" } }
                }
            };

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>(), Arg.Any<bool>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model, useCalculationId);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Facets.Any(f => f == exceptionFilterName)), Arg.Any<bool>());
        }

        [TestMethod]
        public async Task SearchTestScenarioResults_GivenValidModelAndIncludesGettingFacetsAndFiltersIsNull_PerformsSearch()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>(), Arg.Any<bool>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(ProviderCalculationResultsFacetCount)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.SearchFields.Any(f => f == "providerName")));
        }

        [TestMethod]
        [DataRow(true, "calculationId")]
        [DataRow(false, "fundingLineId")]
        [DataRow(false, "fundingLineId", "fieldOne", "fieldTwo")]
        public async Task SearchTestScenarioResults_GivenValidModelWithOneFilter_ThenSearchIsPerformed(
            bool useCalculationId, string idFilterName, params string[] searchFields)
        {
            string[] expectedFacetSearchFields = searchFields.IsNullOrEmpty()
                ? new[]
                {
                    "providerName"
                }
                : searchFields;
            
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                SearchFields = searchFields ?? new string[0],
                Filters = new Dictionary<string, string[]>()
                {
                    { idFilterName, new[]{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>(), Arg.Any<bool>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model, useCalculationId);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            int expectedFacetsSearchCount = expectedFacetSearchFields.SequenceEqual(searchFields ?? ArraySegment<string>.Empty) ? ProviderCalculationResultsFacetCount : ProviderCalculationResultsFacetCount - 1;

            //facet search
            await
               searchRepository
               .Received(expectedFacetsSearchCount)
                   .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                       SearchFiltersMatch(c, model)
                       && SearchFieldsMatch(c.SearchFields, expectedFacetSearchFields)
                   ));
        }

        private bool SearchFiltersMatch(SearchParameters searchParameters,
            SearchModel searchModel)
        {
            bool searchFiltersMatch = searchModel.Filters.Keys.All(_ => searchParameters.Filter.Contains(_)) &&
                                      !string.IsNullOrEmpty(searchParameters.Filter);
            
            return searchFiltersMatch;
        }

        private bool SearchFieldsMatch(IEnumerable<string> actualSearchFields,
            IEnumerable<string> expectedSearchFields)
        {
            bool searchFieldsMatch = actualSearchFields.Count() == expectedSearchFields.Count() &&
                                     expectedSearchFields.All(actualSearchFields.Contains);
            
            return searchFieldsMatch;
        }

        [TestMethod]
        [DataRow(true, "calculationId")]
        [DataRow(false, "fundingLineId")]
        public async Task SearchTestScenarioResults_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed(
            bool useCalculationId, string idFilterName)
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { idFilterName, new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>(), Arg.Any<bool>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model, useCalculationId);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(ProviderCalculationResultsFacetCount)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        [DataRow(true, "calculationId")]
        [DataRow(false, "fundingLineId")]
        public async Task SearchTestScenarioResults_GivenValidModelWithOneFilterAndOverrridesFacets_ThenSearchIsPerformedWithTwoFilters(
            bool useCalculationId, string filterName)
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { filterName, new string []{ "test" } }
                },
                SearchTerm = "testTerm",
                OverrideFacetFields = new[] { "providerId", filterName }
            };

            ILogger logger = CreateLogger();

            SearchResults<ProviderCalculationResultsIndex> searchResults = new SearchResults<ProviderCalculationResultsIndex>();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>(), Arg.Any<bool>())
                .Returns(searchResults);

            ProviderCalculationResultsSearchService service = CreateTestResultsSearchService(logger, searchRepository);

            //Act
            IActionResult result = await service.SearchCalculationProviderResults(model, useCalculationId);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(2)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        static ProviderCalculationResultsSearchService CreateTestResultsSearchService(
          ILogger logger = null,
          ISearchRepository<ProviderCalculationResultsIndex> serachRepository = null,
          IResultsResiliencePolicies resiliencePolicies = null)
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle.IsExceptionMessagesEnabled().Returns(true);
            return new ProviderCalculationResultsSearchService(
                logger ?? CreateLogger(),
                serachRepository ?? CreateSearchRepository(),
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies(),
                featureToggle);
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<ProviderCalculationResultsIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderCalculationResultsIndex>>();
        }
    }
}
