using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.Search;
using CalculateFunding.Models.Policy;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.UnitTests;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using Facet = CalculateFunding.Repositories.Common.Search.Facet;

namespace CalculateFunding.Services.Policy.TemplateBuilderServiceTests
{
    [TestClass]
    public class TemplateSearchServiceTests
    {
        [TestMethod]
        public async Task SearchTemplate_SearchRequestFails_ThenBadRequestReturned()
        {
            SearchModel model = new SearchModel
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                IncludeFacets = false,
                Top = 50,
            };

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            logger
                .Received(1)
                .Error(Arg.Any<FailedToQuerySearchException>(), "Failed to query search with term: SearchTermTest");

            result
                 .Should()
                 .BeOfType<StatusCodeResult>()
                 .Which.StatusCode.Should().Be(500);
        }

        [TestMethod]
        public async Task SearchTemplate_GivenNullSearchModel_LogsAndCreatesDefaultSearchModel()
        {
            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(null);

            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching templates");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTemplate_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            SearchModel model = new SearchModel();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching templates");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTemplate_GivenPageTopIsZero_LogsAndReturnsBadRequest()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching templates");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelAndIncludesGettingFacets_CallsSearchFiveTimes()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(5)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(5)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                        {
                            { "fundingPeriodName", new string []{ "test" } }
                        },
                SearchTerm = "testTerm",
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelWithMultipleFilterValues_ThenSearchIsPerformed()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                        {
                            { "fundingStreamName", new string []{ "test" } }
                        },
                SearchTerm = "testTerm",
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                        {
                            { "fundingStreamName", new string []{ "test", "test2" } }
                        },
                SearchTerm = "testTerm",
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(4)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
        {
            const int skipValue = 50;

            SearchModel model = new SearchModel
            {
                PageNumber = 2,
                Top = 50
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(m => m.Skip == skipValue));
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
        {
            const int skipValue = 450;

            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(m => m.Skip == skipValue));
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidModel_CallsSearchWithCorrectSkipValue()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<TemplateIndex> searchResults = new SearchResults<TemplateIndex>
            {
                Facets = new List<Facet>
                        {
                            new Facet
                            {
                                Name = "specificationNames"
                            }
                        }
            };

            ILogger logger = CreateLogger();

            ISearchRepository<TemplateIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            TemplateSearchService sut = CreateTemplateSearchService(logger: logger, searchRepository: searchRepository);

            IActionResult result = await sut.SearchTemplates(model);

            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(5)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchTemplate_GivenValidParameters_ShouldReturnOkResult()
        {
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50,
                IncludeFacets = false
            };

            SearchResults<TemplateIndex> mockSearchResults = new SearchResults<TemplateIndex>
            {
                Results = new List<Repositories.Common.Search.SearchResult<TemplateIndex>>()
                {
                    CreateTemplateResult(new TemplateIndex
                    {
                        Id = "df073a02-bbc5-44ee-a84b-5931c6e7cf1e-v1",
                        Name = "Template",
                        Version = 2,
                        CurrentMajorVersion = 1,
                        CurrentMinorVersion = 2,
                        PublishedMajorVersion = 1,
                        PublishedMinorVersion = 2,
                        LastUpdatedDate = new DateTime(2019, 1, 1),
                        LastUpdatedAuthorName = "user",
                        LastUpdatedAuthorId = "123",
                        FundingPeriodName = "period",
                        FundingPeriodId = "123",
                        FundingStreamName = "stream",
                        FundingStreamId = "321",
                        HasReleasedVersion = "Yes"
                    })
                }
            };

            ISearchRepository<TemplateIndex> mockTemplateIndexRepository = CreateSearchRepository();
            mockTemplateIndexRepository.Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(mockSearchResults);

            TemplateSearchService sut =
                CreateTemplateSearchService(searchRepository: mockTemplateIndexRepository);

            IActionResult actionResult = await sut.SearchTemplates(model);

            actionResult.Should().BeOfType<OkObjectResult>();

            OkObjectResult objectResult = actionResult as OkObjectResult;
            TemplateSearchResults templateSearchResults = objectResult.Value as TemplateSearchResults;

            templateSearchResults.Results.Count().Should().Be(1);

            TemplateSearchResult templateSearchResult = templateSearchResults.Results.First();
            templateSearchResult.Id.Should().Be("df073a02-bbc5-44ee-a84b-5931c6e7cf1e-v1");
            templateSearchResult.Name.Should().Be("Template");
            templateSearchResult.CurrentMajorVersion.Should().Be(1);
            templateSearchResult.CurrentMinorVersion.Should().Be(2);
            templateSearchResult.PublishedMajorVersion.Should().Be(1);
            templateSearchResult.FundingPeriodName.Should().Be("period");
            templateSearchResult.FundingPeriodId.Should().Be("123");
            templateSearchResult.FundingStreamName.Should().Be("stream");
            templateSearchResult.FundingStreamId.Should().Be("321");
            templateSearchResult.LastUpdatedAuthorName.Should().Be("user");
            templateSearchResult.LastUpdatedAuthorId.Should().Be("123");
            templateSearchResult.LastUpdatedDate.Should().Be(new DateTime(2019, 1, 1));
        }

        [TestMethod]
        public async Task SearchTemplates_GivenPageNumberZero_ReturnsBadRequest()
        {
            SearchModel searchModel = new SearchModel
            {
                PageNumber = 0
            };

            TemplateSearchService sut = CreateTemplateSearchService();

            IActionResult result = await sut.SearchTemplates(searchModel);

            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("An invalid search model was provided");
        }

        [TestMethod]
        public void ReIndex_ThrowsExceptionIfNoUserSupplied()
        {
            TemplateSearchService sut = CreateTemplateSearchService();
            Func<Task<IActionResult>> invocation = () => sut.ReIndex(user: null, correlationId: NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("user");
        }

        [TestMethod]
        public void ReIndex_ThrowsExceptionIfNoCorrelationIdSupplied()
        {
            TemplateSearchService sut = CreateTemplateSearchService();
            Func<Task<IActionResult>> invocation = () => sut.ReIndex(user: NewUser(), correlationId: null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("correlationId");
        }

        [TestMethod]
        public async Task ReIndex_CreatesReIndexTemplatesJob()
        {
            Reference user = NewUser(_ => _.WithId(NewRandomString())
                .WithName(NewRandomString()));
            string correlationId = NewRandomString();

            var jobsApiClient = CreateJobsApiClient();

            TemplateSearchService sut = CreateTemplateSearchService(jobsApiClient: jobsApiClient);

            IActionResult result = await sut.ReIndex(user, correlationId);

            result
                .Should()
                .BeOfType<NoContentResult>();

            await jobsApiClient
                .Received(1)
                .CreateJob(Arg.Is<JobCreateModel>(_ =>
                    _.CorrelationId == correlationId &&
                    _.InvokerUserId == user.Id &&
                    _.InvokerUserDisplayName == user.Name &&
                    _.JobDefinitionId == JobConstants.DefinitionNames.ReIndexTemplatesJob));
        }

        private Reference NewUser(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder userBuilder = new ReferenceBuilder();

            setUp?.Invoke(userBuilder);

            return userBuilder.Build();
        }

        private string NewRandomString() => new RandomString();

        private static Repositories.Common.Search.SearchResult<TemplateIndex> CreateTemplateResult(TemplateIndex templateIndex)
        {
            return new Repositories.Common.Search.SearchResult<TemplateIndex>()
            {
                Result = templateIndex
            };
        }

        static TemplateSearchService CreateTemplateSearchService(
            ILogger logger = null,
            ISearchRepository<TemplateIndex> searchRepository = null,
            IJobsApiClient jobsApiClient = null)
        {
            return new TemplateSearchService(
                logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                new PolicyResiliencePolicies
                {
                    JobsApiClient = Polly.Policy.NoOpAsync()
                },
                jobsApiClient ?? CreateJobsApiClient());
        }

        static IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
            ;
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<TemplateIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<TemplateIndex>>();
        }
    }
}
