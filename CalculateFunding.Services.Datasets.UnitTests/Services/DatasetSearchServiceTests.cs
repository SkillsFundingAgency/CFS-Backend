using System;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Datasets;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Repositories.Common.Search.Results;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class DatasetSearchServiceTests
    {
        [TestMethod]
        public async Task SearchDataset_SearchRequestFails_ThenBadRequestReturned()
        {
            //Arrange
            SearchModel model = new SearchModel()
            {
                SearchTerm = "SearchTermTest",
                PageNumber = 1,
                IncludeFacets = false,
                Top = 50,
            };

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });


            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

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
        public async Task SearchDataset_GivenNullSearchModel_LogsAndCreatesDefaultSearchModel()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(null);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching datasets");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchDataset_GivenPageNumberIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching datasets");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchDataset_GivenPageTopIsZero_LogsAndReturnsBadRequest()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 0
            };

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            logger
                .Received(1)
                .Error("A null or invalid search model was provided for searching datasets");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelAndIncludesGettingFacets_CallsSearchSevenTimes()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelWithNullFilters_ThenSearchIsStillPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = null,
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());

        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelWithOneFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "status", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelWithOneFilterWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "specificationNames", new string []{ "test" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelWithMultipleFilterValuesWhichIsMultiValueFacet_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "specificationNames", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "status", new string []{ "test", "test2" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelWithNullFilterWithMultipleOfSameFilter_ThenSearchIsPerformed()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50,
                IncludeFacets = true,
                Filters = new Dictionary<string, string[]>()
                {
                    { "status", new string []{ "test", "" } }
                },
                SearchTerm = "testTerm",
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                .Received(6)
                    .Search(model.SearchTerm, Arg.Is<SearchParameters>(c =>
                        model.Filters.Keys.All(f => c.Filter.Contains(f))
                        && !string.IsNullOrWhiteSpace(c.Filter)
                    ));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelAndDoesntIncludeGettingFacets_CallsSearchOnce()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 1,
                Top = 50
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelAndPageNumber2_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 50;

            SearchModel model = new SearchModel
            {
                PageNumber = 2,
                Top = 50
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(m => m.Skip == skipValue));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModelAndPageNumber10_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            const int skipValue = 450;

            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>();

            ILogger logger = CreateLogger();

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(m => m.Skip == skipValue));
        }

        [TestMethod]
        public async Task SearchDataset_GivenValidModel_CallsSearchWithCorrectSkipValue()
        {
            //Arrange
            SearchModel model = new SearchModel
            {
                PageNumber = 10,
                Top = 50,
                IncludeFacets = true
            };

            SearchResults<DatasetIndex> searchResults = new SearchResults<DatasetIndex>
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

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            DatasetSearchService service = CreateDatasetSearchService(logger: logger, serachRepository: searchRepository);

            //Act
            IActionResult result = await service.SearchDatasets(model);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(7)
                    .Search(Arg.Any<string>(), Arg.Any<SearchParameters>());
        }

	    [TestMethod]
	    public async Task SearchDatasetVersion_GivenValidParameters_ShouldReturnOkResult()
	    {
			// Arrange
		    SearchModel model = new SearchModel
		    {
			    PageNumber = 10,
			    Top = 50,
			    IncludeFacets = false
		    };

		    string blobName = "v1/Pe and sports Data.xlsx";

			SearchResults<DatasetVersionIndex> mockSearchResults = new SearchResults<DatasetVersionIndex>();
		    mockSearchResults.Results = new List<Repositories.Common.Search.SearchResult<DatasetVersionIndex>>()
		    {
			    CreateDatasetVersionResult(new DatasetVersionIndex()
			    {
				    Id = "df073a02-bbc5-44ee-a84b-5931c6e7cf1e-v1",
				    Name = "Pe and sports Data",
				    Version = 1,
				    BlobName = blobName,
				    DefinitionName = "PSG",
				    DatasetId = "df073a02-bbc5-44ee-a84b-5931c6e7cf1e",
				    Description = "150 rows starting",
				    LastUpdatedByName = "James",
				    LastUpdatedDate = new DateTime(2019, 1, 1),
                    FundingStreamId = "DSG",
                    FundingStreamName = "Dedicated schools grant"
                })
		    };

			ISearchRepository<DatasetVersionIndex> mockDatasetVersionIndexRepository = CreateDatasetVersionSearchRepository();
		    mockDatasetVersionIndexRepository.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()).Returns(mockSearchResults);
		    DatasetSearchService service = CreateDatasetSearchService(searchRepositoryDatasetVersion: mockDatasetVersionIndexRepository);

			// Act
		    IActionResult actionResult = await service.SearchDatasetVersion(model);
			
		    // Assert
		    actionResult.Should().BeOfType<OkObjectResult>();

		    OkObjectResult objectResult = actionResult as OkObjectResult;
		    DatasetVersionSearchResults datasetVersionSearchResults = objectResult.Value as DatasetVersionSearchResults;

		    datasetVersionSearchResults.Results.Count().Should().Be(1);

		    DatasetVersionSearchResult datasetVersionSearchResult = datasetVersionSearchResults.Results.First();
		    datasetVersionSearchResult.Id.Should().Be("df073a02-bbc5-44ee-a84b-5931c6e7cf1e-v1");
		    datasetVersionSearchResult.Name.Should().Be("Pe and sports Data");
		    datasetVersionSearchResult.Version.Should().Be(1);
		    datasetVersionSearchResult.BlobName.Should().Be(blobName);
		    datasetVersionSearchResult.DefinitionName.Should().Be("PSG");
		    datasetVersionSearchResult.DatasetId.Should().Be("df073a02-bbc5-44ee-a84b-5931c6e7cf1e");
		    datasetVersionSearchResult.Description.Should().Be("150 rows starting");
		    datasetVersionSearchResult.LastUpdatedByName.Should().Be("James");
		    datasetVersionSearchResult.LastUpdatedDate.Should().Be(new DateTime(2019, 1, 1));
            datasetVersionSearchResult.FundingStreamId.Should().Be("DSG");
            datasetVersionSearchResult.FundingStreamName.Should().Be("Dedicated schools grant");
        }

        [TestMethod]
		public async Task SearchDatasetVersion_GivenInvalidParameters_ShouldReturnBadRequestResult()
		{
			// Arrange
			SearchModel model = new SearchModel
			{
				PageNumber = 0,
				Top = 0,
				IncludeFacets = false
			};

			SearchResults<DatasetVersionIndex> mockSearchResults = new SearchResults<DatasetVersionIndex>();

			ISearchRepository<DatasetVersionIndex> mockDatasetVersionIndexRepository = CreateDatasetVersionSearchRepository();
			mockDatasetVersionIndexRepository.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()).Returns(mockSearchResults);

			DatasetSearchService service = CreateDatasetSearchService(searchRepositoryDatasetVersion: mockDatasetVersionIndexRepository);

			// Act
			IActionResult actionResult = await service.SearchDatasetVersion(model);

			// Assert
			actionResult
				.Should().BeOfType<BadRequestObjectResult>()
				.Which
				.Value
				.Should().Be("An invalid search model was provided");
		}

		private static Repositories.Common.Search.SearchResult<DatasetVersionIndex> CreateDatasetVersionResult(DatasetVersionIndex datasetVersionIndex)
	    {
		    return new Repositories.Common.Search.SearchResult<DatasetVersionIndex>()
		    {
			    Result = datasetVersionIndex
		    };
	    }


	    static DatasetSearchService CreateDatasetSearchService(
		    ILogger logger = null,
		    ISearchRepository<DatasetIndex> serachRepository = null,
		    ISearchRepository<DatasetVersionIndex> searchRepositoryDatasetVersion = null)
	    {
		    return new DatasetSearchService(
			    logger ?? CreateLogger(),
			    serachRepository ?? CreateSearchRepository(),
			    searchRepositoryDatasetVersion ?? CreateDatasetVersionSearchRepository());
	    }

	    static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ISearchRepository<DatasetIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetIndex>>();
        }

	    static ISearchRepository<DatasetVersionIndex> CreateDatasetVersionSearchRepository()
	    {
		    return Substitute.For<ISearchRepository<DatasetVersionIndex>>();
	    }
    }
}
