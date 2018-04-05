using AutoMapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using CalculateFunding.Services.Core.Interfaces.Logging;

namespace CalculateFunding.Services.Results.Services
{
    [TestClass]
    public class ResultsServiceTests
    {
        const string providerId = "123456";
        const string specificationId = "888999";

        [TestMethod]
        async public Task GetProviderById_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderResults"));
        }

        [TestMethod]
        async public Task GetProviderById_GivenProviderNotFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(providerId), IdFieldOverride: Arg.Is("ukPrn"))
                .Returns((ProviderIndex)null);

            ResultsService service = CreateResultsService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.GetProviderById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        async public Task GetProviderById_GivenProviderFound_ReturnsOKResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) }

            });

            ProviderIndex provider = new ProviderIndex();

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(providerId), IdFieldOverride: Arg.Is("ukPrn"))
                .Returns(provider);

            ResultsService service = CreateResultsService(logger: logger, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.GetProviderById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task GetProviderResults_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderResults(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderResults"));
        }

        [TestMethod]
        async public Task GetProviderResults_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderResults(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetProviderResults"));
        }

        [TestMethod]
        async public Task GetProviderResults_GivenNullProviderResultReturned_ReturnsNotFoundResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) },
                { "specificationId", new StringValues(specificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResult(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns((ProviderResult)null);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderResults(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A result was not found for provider id {providerId}, specification id {specificationId}"));
        }

        [TestMethod]
        async public Task GetProviderResults_GivenProviderResultReturned_ReturnsOK()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) },
                { "specificationId", new StringValues(specificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ProviderResult providerResult = new ProviderResult();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResult(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns(providerResult);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderResults(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task GetProviderSpecifications_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderSpecifications"));
        }

        [TestMethod]
        async public Task GetProviderSpecifications_GivenEmptyProviderResultsReturned_ReturnsOKWithEmptyCollection()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IEnumerable<ProviderResult> providerResults = Enumerable.Empty<ProviderResult>();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSpecificationResults(Arg.Is(providerId))
                .Returns(providerResults);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResults = result as OkObjectResult;
            okResults
                .Value
                .Should()
                .NotBeNull();

            IEnumerable<SpecificationSummary> summaries = okResults.Value as IEnumerable<SpecificationSummary>;
            summaries
                .Count()
                .Should()
                .Be(0);

            logger
                .Received(1)
                .Information(Arg.Is($"Results were not found for provider id {providerId}"));
        }

        [TestMethod]
        async public Task GetProviderSpecifications_GivenProviderResultsReturned_ReturnsOK()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IEnumerable<ProviderResult> providerResults = new[]
            {
                new ProviderResult
                {
                    Specification = new SpecificationSummary
                    {
                        Id = specificationId
                    }
                }
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSpecificationResults(Arg.Is(providerId))
                .Returns(providerResults);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResults = result as OkObjectResult;

            IEnumerable<SpecificationSummary> summaries = okResults.Value as IEnumerable<SpecificationSummary>;

            summaries
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        async public Task GetProviderSpecifications_GivenProviderResultsWithDuplicateSummariesReturned_ReturnsOK()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "providerId", new StringValues(providerId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IEnumerable<ProviderResult> providerResults = new[]
            {
                new ProviderResult
                {
                    Specification = new SpecificationSummary
                    {
                        Id = specificationId
                    }
                },
                new ProviderResult
                {
                    Specification = new SpecificationSummary
                    {
                        Id = specificationId
                    }
                },
                new ProviderResult
                {
                    Specification = new SpecificationSummary
                    {
                        Id = "another-spec-id"
                    }
                }
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSpecificationResults(Arg.Is(providerId))
                .Returns(providerResults);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderSpecifications(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResults = result as OkObjectResult;

            IEnumerable<SpecificationSummary> summaries = okResults.Value as IEnumerable<SpecificationSummary>;

            summaries
                .Count()
                .Should()
                .Be(2);
        }

        [TestMethod]
        async public Task GetProviderResultsBySpecificationId_GivenNoSpecificationIsProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger);

            //Act
            IActionResult result = await service.GetProviderResultsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        async public Task GetProviderResultsBySpecificationId_GivenSpecificationIsProvided_ReturnsResults()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IEnumerable<ProviderResult> providerResults = new[]
            {
                new ProviderResult(),
                new ProviderResult()
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResultsBySpecificationId(Arg.Is(specificationId))
                .Returns(providerResults);

            ResultsService service = CreateResultsService(logger, resultsRepository: resultsRepository);

            //Act
            IActionResult result = await service.GetProviderResultsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResults = result as OkObjectResult;

            IEnumerable<ProviderResult> okResultsValue = okResults.Value as IEnumerable<ProviderResult>;

            okResultsValue
                .Count()
                .Should()
                .Be(2);
        }

        [TestMethod]
        public void UpdateProviderData_GivenNullResults_ThrowsArgumentNullException()
        {
            //Arrange
            EventData message = new EventData(new byte[0]);

            ResultsService service = CreateResultsService();

            //Act
            Func<Task> test = () => service.UpdateProviderData(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        //[TestMethod]
        //async public Task UpdateProviderData_GivenEmptyResults_DoesNotUpdateResults()
        //{
        //    //Arramge
        //    IEnumerable<ProviderResult> results = Enumerable.Empty<ProviderResult>();

        //    var json = JsonConvert.SerializeObject(results);

        //    EventData message = new EventData(Encoding.UTF8.GetBytes(json));

        //    ILogger logger = CreateLogger();

        //    IResultsRepository resultsRepository = CreateResultsRepository();

        //    ResultsService service = CreateResultsService(logger, resultsRepository);

        //    //Act
        //    await service.UpdateProviderData(message);

        //    //Assert
        //    await
        //        resultsRepository
        //            .DidNotReceive()
        //            .UpdateProviderResults(Arg.Any<List<ProviderResult>>());

        //    logger
        //        .Received(1)
        //        .Warning("An empty list of results were provided to update");
        //}

        //[TestMethod]
        //async public Task UpdateProviderData_GivenResultsButFailsToUpdate_LogsError()
        //{
        //    //Arramge
        //    IEnumerable<ProviderResult> results = new[]
        //    {
        //        new ProviderResult()
        //    };

        //    var json = JsonConvert.SerializeObject(results);

        //    EventData message = new EventData(Encoding.UTF8.GetBytes(json));

        //    ILogger logger = CreateLogger();

        //    IResultsRepository resultsRepository = CreateResultsRepository();
        //    resultsRepository
        //        .UpdateProviderResults(Arg.Any<List<ProviderResult>>())
        //        .Returns(HttpStatusCode.InternalServerError);

        //    ResultsService service = CreateResultsService(logger, resultsRepository);

        //    //Act
        //    await service.UpdateProviderData(message);

        //    //Assert
        //    logger
        //        .Received(1)
        //        .Error("Failed to bulk update provider data with status code: InternalServerError");
        //}

        //[TestMethod]
        //async public Task UpdateProviderData_GivenResultsAndupdateIsSuccess_DoesNotLogAnError()
        //{
        //    //Arramge
        //    IEnumerable<ProviderResult> results = new[]
        //    {
        //        new ProviderResult()
        //    };

        //    var json = JsonConvert.SerializeObject(results);

        //    EventData message = new EventData(Encoding.UTF8.GetBytes(json));

        //    ILogger logger = CreateLogger();

        //    IResultsRepository resultsRepository = CreateResultsRepository();
        //    resultsRepository
        //        .UpdateProviderResults(Arg.Any<List<ProviderResult>>())
        //        .Returns(HttpStatusCode.OK);

        //    ResultsService service = CreateResultsService(logger, resultsRepository);

        //    //Act
        //    await service.UpdateProviderData(message);

        //    //Assert
        //    logger
        //        .DidNotReceive()
        //        .Error(Arg.Any<string>());
        //}

        [TestMethod]
        public void UpdateProviderSourceDataset_GivenNullResults_ThrowsArgumentNullException()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ResultsService service = CreateResultsService();

            //Act
            Func<Task> test = () => service.UpdateProviderSourceDataset(request);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        async public Task UpdateProviderSourceDataset_GivenUpdatingDatasetFailsWithInternalServerError_ReturnsStatusCode500()
        {
            //Arrange
            ProviderSourceDataset model = new ProviderSourceDataset();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IProviderSourceDatasetRepository providerSourceDatasetRepository = CreateProviderSourceDatasetRepository();
            providerSourceDatasetRepository
                 .UpsertProviderSourceDataset(Arg.Any<ProviderSourceDataset>())
                .Returns(HttpStatusCode.InternalServerError);

            ResultsService service = CreateResultsService(logger, providerSourceDatasetRepository: providerSourceDatasetRepository);

            //Act
            IActionResult result = await service.UpdateProviderSourceDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error("Failed to update provider source dataset with status code: 500");
        }

        [TestMethod]
        async public Task UpdateProviderSourceDataset_GivenUpdatingDatasetSucceeds_ReturnsNoContent()
        {
            //Arrange
            ProviderSourceDataset model = new ProviderSourceDataset();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IProviderSourceDatasetRepository providerSourceDatasetRepository = CreateProviderSourceDatasetRepository();
            providerSourceDatasetRepository
                .UpsertProviderSourceDataset(Arg.Any<ProviderSourceDataset>())
                .Returns(HttpStatusCode.OK);

            ResultsService service = CreateResultsService(logger, providerSourceDatasetRepository: providerSourceDatasetRepository);

            //Act
            IActionResult result = await service.UpdateProviderSourceDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        async public Task GetProviderSourceDatasetsByProviderIdAndSpecificationId_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger);

            //Act
            IActionResult result = await service.GetProviderSourceDatasetsByProviderIdAndSpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetProviderResultsBySpecificationId"));
        }

        [TestMethod]
        async public Task GetProviderSourceDatasetsByProviderIdAndSpecificationId_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger);

            //Act
            IActionResult result = await service.GetProviderSourceDatasetsByProviderIdAndSpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderResultsBySpecificationId"));
        }

        [TestMethod]
        async public Task GetProviderSourceDatasetsByProviderIdAndSpecificationId_GivenResultsReturned_ReturnsOKResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
                { "providerId", new StringValues(providerId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IEnumerable<ProviderSourceDataset> providerSources = new[] { new ProviderSourceDataset(), new ProviderSourceDataset() };

            IProviderSourceDatasetRepository providerSourceDatasetRepository = CreateProviderSourceDatasetRepository();
            providerSourceDatasetRepository
                .GetProviderSourceDatasets(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns(providerSources);

            ResultsService service = CreateResultsService(logger, providerSourceDatasetRepository: providerSourceDatasetRepository);

            //Act
            IActionResult result = await service.GetProviderSourceDatasetsByProviderIdAndSpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<ProviderSourceDataset> sourceDatasets = okResult.Value as IEnumerable<ProviderSourceDataset>;

            sourceDatasets
                .Count()
                .Should()
                .Be(2);
        }

        static ResultsService CreateResultsService(ILogger logger = null,
            ICalculationResultsRepository resultsRepository = null,
            IMapper mapper = null,
            ISearchRepository<ProviderIndex> searchRepository = null,
            IMessengerService messengerService = null,
            EventHubSettings EventHubSettings = null,
            ITelemetry telemetry = null,
            IProviderSourceDatasetRepository providerSourceDatasetRepository = null)
        {
            return new ResultsService(
                logger ?? CreateLogger(),
                resultsRepository ?? CreateResultsRepository(),
                mapper ?? CreateMapper(),
                searchRepository ?? CreateSearchRepository(),
                messengerService ?? CreateMessengerService(),
                EventHubSettings ?? CreateEventHubSettings(),
                telemetry ?? CreateTelemetry(),
                providerSourceDatasetRepository ?? CreateProviderSourceDatasetRepository());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static ICalculationResultsRepository CreateResultsRepository()
        {
            return Substitute.For<ICalculationResultsRepository>();
        }

        static IProviderSourceDatasetRepository CreateProviderSourceDatasetRepository()
        {
            return Substitute.For<IProviderSourceDatasetRepository>();
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        static ISearchRepository<ProviderIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderIndex>>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static EventHubSettings CreateEventHubSettings()
        {
            return new EventHubSettings();
        }
    }
}
