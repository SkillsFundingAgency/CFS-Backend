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
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.UnitTests;

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
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<string>>()
                .Which
                .Should()
                .HaveCount(0);

            logger
                .Received(1)
                .Information(Arg.Is($"Results were not found for provider id '{providerId}'"));
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
                    SpecificationId = specificationId,
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
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<string>>()
                .Which
                .Should()
                .HaveCount(1);
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
                    SpecificationId = specificationId,
                },
                new ProviderResult
                {
                    SpecificationId = specificationId,
                },
                new ProviderResult
                {
                    SpecificationId = "another-spec-id",
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
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<string>>()
                .Which
                .Should()
                .HaveCount(2);
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
        async public Task GetProviderResultsBySpecificationId_GivenSpecificationIsProvidedAndTopIsProvided_ReturnsResults()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) },
                { "top", new StringValues("1") }
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
                .GetProviderResultsBySpecificationId(Arg.Is(specificationId), Arg.Is(1))
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
            Message message = new Message(new byte[0]);

            ResultsService service = CreateResultsService();

            //Act
            Func<Task> test = () => service.UpdateProviderData(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

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

        [TestMethod]
        public async Task ReIndexCalculationProviderResults_GivenResultReturnedFromDatabaseWithTwoCalcResultsButSearchRetuensErrors_ReturnsStatusCode500()
        {
            //Arrange
            DocumentEntity<ProviderResult> providerResult = CreateDocumentEntity();

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .GetAllProviderResults()
                .Returns(new[] { providerResult });

            ILogger logger = CreateLogger();

            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<CalculationProviderResultsIndex>>())
                .Returns(new[] { new IndexError { ErrorMessage = "an error" } });

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = providerResult.Content.SpecificationId,
                Name = "spec name",
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specificationSummary.Id))
                .Returns(specificationSummary);

            ResultsService resultsService = CreateResultsService(
                resultsRepository: calculationResultsRepository, 
                calculationProviderResultsSearchRepository: searchRepository, 
                specificationsRepository : specificationsRepository,
                logger: logger);

            //Act
            IActionResult actionResult = await resultsService.ReIndexCalculationProviderResults();

            //Assert
            actionResult
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = actionResult as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Is("Failed to index calculation provider result documents with errors: an error"));
        }

        [TestMethod]
        public async Task ReIndexCalculationProviderResults_GivenResultReturnedFromDatabaseWithTwoCalcResults_UpdatesSearchWithTwoDocumentsReturnsNoContent()
        {
            //Arrange
            DocumentEntity<ProviderResult> providerResult = CreateDocumentEntity();

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .GetAllProviderResults()
                .Returns(new[] { providerResult });

            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = providerResult.Content.SpecificationId,
                Name = "spec name",
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specificationSummary.Id))
                .Returns(specificationSummary);

            ResultsService resultsService = CreateResultsService(
                resultsRepository: calculationResultsRepository, 
                calculationProviderResultsSearchRepository: searchRepository,
                specificationsRepository :  specificationsRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexCalculationProviderResults();

            //Assert
            actionResult
                .Should()
                .BeOfType<NoContentResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(m => m.Count() == 2));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(
                        m =>
                            m.First().SpecificationId == "spec-id" &&
                            m.First().SpecificationName == "spec name" &&
                            m.First().CalculationSpecificationId == "calc-spec-id-1" &&
                            m.First().CalculationSpecificationName == "calc spec name 1" &&
                            m.First().CaclulationResult == 123 &&
                            m.First().CalculationType == "Funding" &&
                            m.First().ProviderId == "prov-id" &&
                            m.First().ProviderName == "prov name" &&
                            m.First().ProviderType == "prov type" &&
                            m.First().ProviderSubType == "prov sub type" &&
                            m.First().UKPRN == "ukprn" &&
                            m.First().UPIN == "upin" &&
                            m.First().URN == "urn" &&
                            m.First().EstablishmentNumber == "12345"
                    ));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(
                        m =>
                            m.Last().SpecificationId == "spec-id" &&
                            m.Last().SpecificationName == "spec name" &&
                            m.Last().CalculationSpecificationId == "calc-spec-id-2" &&
                            m.Last().CalculationSpecificationName == "calc spec name 2" &&
                            m.Last().CaclulationResult == 10 &&
                            m.Last().CalculationType == "Number" &&
                            m.Last().ProviderId == "prov-id" &&
                            m.Last().ProviderName == "prov name" &&
                            m.Last().ProviderType == "prov type" &&
                            m.Last().ProviderSubType == "prov sub type" &&
                            m.Last().UKPRN == "ukprn" &&
                            m.Last().UPIN == "upin" &&
                            m.Last().URN == "urn" &&
                            m.Last().EstablishmentNumber == "12345"
                    ));
        }

        static ResultsService CreateResultsService(ILogger logger = null,
            ICalculationResultsRepository resultsRepository = null,
            IMapper mapper = null,
            ISearchRepository<ProviderIndex> searchRepository = null,
            IMessengerService messengerService = null,
            ServiceBusSettings EventHubSettings = null,
            ITelemetry telemetry = null,
            IProviderSourceDatasetRepository providerSourceDatasetRepository = null,
            ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository = null,
            ISpecificationsRepository specificationsRepository = null,
            IResultsResilliencePolicies resiliencePolicies = null,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService = null,
            IPublishedProviderResultsRepository publishedProviderResultsRepository = null,
            IPublishedProviderCalculationResultsRepository IPublishedProviderCalculationResultsRepository = null)
        {
            return new ResultsService(
                logger ?? CreateLogger(),
                resultsRepository ?? CreateResultsRepository(),
                mapper ?? CreateMapper(),
                searchRepository ?? CreateSearchRepository(),
                messengerService ?? CreateMessengerService(),
                EventHubSettings ?? CreateEventHubSettings(),
                telemetry ?? CreateTelemetry(),
                providerSourceDatasetRepository ?? CreateProviderSourceDatasetRepository(),
                calculationProviderResultsSearchRepository ?? CreateCalculationProviderResultsSearchRepository(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies(),
                publishedProviderResultsAssemblerService ?? CreateResultsAssembler(),
                publishedProviderResultsRepository ?? CreatePublishedProviderResultsRepository(),
                IPublishedProviderCalculationResultsRepository ?? CreatePublishedProviderCalculationResultsRepository());
        }

        static IPublishedProviderCalculationResultsRepository CreatePublishedProviderCalculationResultsRepository()
        {
            return Substitute.For<IPublishedProviderCalculationResultsRepository>();
        }

        static IPublishedProviderResultsAssemblerService CreateResultsAssembler()
        {
            return Substitute.For<IPublishedProviderResultsAssemblerService>();
        }

        static IPublishedProviderResultsRepository CreatePublishedProviderResultsRepository()
        {
            return Substitute.For<IPublishedProviderResultsRepository>();
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

        static ServiceBusSettings CreateEventHubSettings()
        {
            return new ServiceBusSettings();
        }

        static ISearchRepository<CalculationProviderResultsIndex> CreateCalculationProviderResultsSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationProviderResultsIndex>>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static DocumentEntity<ProviderResult> CreateDocumentEntity()
        {
            return new DocumentEntity<ProviderResult>
            {
                UpdatedAt = DateTime.Now,
                Content = new ProviderResult
                {
                    SpecificationId = "spec-id",
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = 123,
                            CalculationType = CalculationType.Funding
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = CalculationType.Number
                        }
                    },
                    Provider = new ProviderSummary
                    {
                        Id = "prov-id",
                        Name = "prov name",
                        ProviderType = "prov type",
                        ProviderSubType = "prov sub type",
                        Authority = "authority",
                        UKPRN = "ukprn",
                        UPIN = "upin",
                        URN = "urn",
                        EstablishmentNumber = "12345",
                        DateOpened = DateTime.Now.AddDays(-7)
                    }
                }
            };
        }
    }
}
