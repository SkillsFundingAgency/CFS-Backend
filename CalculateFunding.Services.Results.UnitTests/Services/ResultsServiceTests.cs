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
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Results.Services
{
    [TestClass]
    public partial class ResultsServiceTests
    {
        const string providerId = "123456";
        const string specificationId = "888999";
        const string fundingStreamId = "fs-1";
        const string fundingPeriodId = "fp-1";

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
                .Error(Arg.Is("No provider Id was provided to GetProviderById"));
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
                .SearchById(Arg.Is(providerId), IdFieldOverride: Arg.Is("providerId"))
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
                .Should()
                .ThrowExactly<ArgumentNullException>();
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
                            m.First().CalculationResult == 123 &&
                            m.First().CalculationType == "Funding" &&
                            m.First().ProviderId == "prov-id" &&
                            m.First().ProviderName == "prov name" &&
                            m.First().ProviderType == "prov type" &&
                            m.First().ProviderSubType == "prov sub type" &&
                            m.First().UKPRN == "ukprn" &&
                            m.First().UPIN == "upin" &&
                            m.First().URN == "urn" &&
                            m.First().EstablishmentNumber == "12345" &&
                            m.First().IsExcluded == false
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
                            m.Last().CalculationResult == 10 &&
                            m.Last().CalculationType == "Number" &&
                            m.Last().ProviderId == "prov-id" &&
                            m.Last().ProviderName == "prov name" &&
                            m.Last().ProviderType == "prov type" &&
                            m.Last().ProviderSubType == "prov sub type" &&
                            m.Last().UKPRN == "ukprn" &&
                            m.Last().UPIN == "upin" &&
                            m.Last().URN == "urn" &&
                            m.Last().EstablishmentNumber == "12345" &&
                            m.Last().IsExcluded == false
                    ));
        }

        [TestMethod]
        public async Task ReIndexCalculationProviderResults_GivenResultReturnedFromDatabaseWithCalcResultWithNullValue_UpdatesSearch_AndSetsIsExcluded_ThenReturnsNoContent()
        {
            //Arrange
            DocumentEntity<ProviderResult> providerResult = CreateDocumentEntityWithNullCalculationResult();

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .GetAllProviderResults()
                .Returns(new[] { providerResult });

            ISearchRepository<CalculationProviderResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
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
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult actionResult = await resultsService.ReIndexCalculationProviderResults();

            //Assert
            actionResult
                .Should()
                .BeOfType<NoContentResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(m => m.Count() == 1));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<CalculationProviderResultsIndex>>(
                        m =>
                            m.First().SpecificationId == "spec-id" &&
                            m.First().SpecificationName == "spec name" &&
                            m.First().CalculationSpecificationId == "calc-spec-id-1" &&
                            m.First().CalculationSpecificationName == "calc spec name 1" &&
                            m.First().CalculationResult == null &&
                            m.First().CalculationType == "Funding" &&
                            m.First().ProviderId == "prov-id" &&
                            m.First().ProviderName == "prov name" &&
                            m.First().ProviderType == "prov type" &&
                            m.First().ProviderSubType == "prov sub type" &&
                            m.First().UKPRN == "ukprn" &&
                            m.First().UPIN == "upin" &&
                            m.First().URN == "urn" &&
                            m.First().EstablishmentNumber == "12345" &&
                            m.First().IsExcluded == true
                    ));
        }

        [TestMethod]
        public async Task ImportProviders_GivenNullProviders_ReturnsBadRequestObject()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await resultsService.ImportProviders(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("No providers were provided");

            logger
                .Received(1)
                .Error(Arg.Is("No providers were provided"));
        }

        [TestMethod]
        public async Task ImportProviders_GivenInvalidProviders_ReturnsBadRequestObject()
        {
            //Arrange
            string[] providers = { "one", "two" };
            string json = JsonConvert.SerializeObject(providers);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await resultsService.ImportProviders(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Invalid providers were provided");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Invalid providers were provided"));
        }

        [TestMethod]
        public async Task ImportProviders_GivenErrorOccurredDuringIndexing_ReturnsInternalServerError()
        {
            //Arrange
            IEnumerable<MasterProviderModel> providers = CreateProviderModels();
            string json = JsonConvert.SerializeObject(providers);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IEnumerable<IndexError> errors = new[]
            {
                new IndexError{ ErrorMessage = "errored" }
            };

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<ProviderIndex>>())
                .Returns(errors);

            ResultsService resultsService = CreateResultsService(logger, searchRepository: searchRepository);

            const string expectedErrorMessage = "Failed to index providers result documents with errors: errored";

            //Act
            IActionResult result = await resultsService.ImportProviders(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(expectedErrorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(expectedErrorMessage));
        }

        [TestMethod]
        public async Task ImportProviders_GivenProvidersIndexed_ReturnsNoContentResult()
        {
            //Arrange
            IEnumerable<MasterProviderModel> providers = CreateProviderModels();
            string json = JsonConvert.SerializeObject(providers);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<ProviderIndex>>())
                .Returns(Enumerable.Empty<IndexError>());


            IProviderImportMappingService mappingService = CreateProviderImportMappingService();
            mappingService
                .Map(Arg.Any<MasterProviderModel>())
                .Returns(new ProviderIndex());
            mappingService
               .Map(Arg.Any<MasterProviderModel>())
               .Returns(new ProviderIndex());
            mappingService
               .Map(Arg.Any<MasterProviderModel>())
               .Returns(new ProviderIndex());

            ResultsService resultsService = CreateResultsService(searchRepository: searchRepository, providerImportMappingService: mappingService);

            //Act
            IActionResult result = await resultsService.ImportProviders(request);

            //Assert
            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<ProviderIndex>>(m => m.Count() == 3));

            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task ImportProviders_GivenProvidersWithDifferentDateFormatsIndexed_ReturnsNoContentResult()
        {
            //Arrange
            StringBuilder json = new StringBuilder();
            json.AppendLine(@"[{");
            json.AppendLine(@"        ""MasterURN"":  ""100000"",");
            json.AppendLine(@"        ""MasterUPIN"":  """",");
            json.AppendLine(@"        ""MasterProviderStatusName"":  ""Open"",");
            json.AppendLine(@"        ""MasterLocalAuthorityCode"":  ""201"",");
            json.AppendLine(@"        ""MasterDfELAEstabNo"":  ""2013614"",");
            json.AppendLine(@"        ""MasterProviderLegalName"":  """",");
            json.AppendLine(@"        ""MasterProviderTypeName"":  ""Voluntary aided school"",");
            json.AppendLine(@"        ""MasterDateOpened"":  ""01-01-1920"",");
            json.AppendLine(@"        ""MasterDfEEstabNo"":  ""3614"",");
            json.AppendLine(@"        ""MasterUKPRN"":  ""10079319"",");
            json.AppendLine(@"        ""MasterCRMAccountId"":  """",");
            json.AppendLine(@"        ""MasterNavendorNo"":  """",");
            json.AppendLine(@"        ""MasterPhaseOfEducation"":  null,");
            json.AppendLine(@"        ""MasterProviderName"":  ""Sir John Cass\u0027s Foundation Primary School"",");
            json.AppendLine(@"        ""MasterProviderTypeGroupName"":  ""LA maintained schools"",");
            json.AppendLine(@"        ""MasterLocalAuthorityName"":  ""City of London"",");
            json.AppendLine(@"        ""MasterDateClosed"":  """"");
            json.AppendLine(@"    },");
            json.AppendLine(@"    {");
            json.AppendLine(@"        ""MasterURN"":  ""100001"",");
            json.AppendLine(@"        ""MasterUPIN"":  """",");
            json.AppendLine(@"        ""MasterProviderStatusName"":  ""Open"",");
            json.AppendLine(@"        ""MasterLocalAuthorityCode"":  ""201"",");
            json.AppendLine(@"        ""MasterDfELAEstabNo"":  ""2016005"",");
            json.AppendLine(@"        ""MasterProviderLegalName"":  """",");
            json.AppendLine(@"        ""MasterProviderTypeName"":  ""Other independent school"",");
            json.AppendLine(@"        ""MasterDateOpened"":  ""01/01/1920"",");
            json.AppendLine(@"        ""MasterDfEEstabNo"":  ""6005"",");
            json.AppendLine(@"        ""MasterUKPRN"":  ""10013279"",");
            json.AppendLine(@"        ""MasterCRMAccountId"":  """",");
            json.AppendLine(@"        ""MasterNavendorNo"":  """",");
            json.AppendLine(@"        ""MasterPhaseOfEducation"":  null,");
            json.AppendLine(@"        ""MasterProviderName"":  ""City of London School for Girls"",");
            json.AppendLine(@"        ""MasterProviderTypeGroupName"":  ""Independent schools"",");
            json.AppendLine(@"        ""MasterLocalAuthorityName"":  ""City of London"",");
            json.AppendLine(@"        ""MasterDateClosed"":  """"");
            json.AppendLine(@"    }]");

            byte[] byteArray = Encoding.UTF8.GetBytes(json.ToString());
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<ProviderIndex>>())
                .Returns(Enumerable.Empty<IndexError>());


            IProviderImportMappingService mappingService = CreateProviderImportMappingService();
            mappingService
                .Map(Arg.Any<MasterProviderModel>())
                .Returns(new ProviderIndex());
            mappingService
               .Map(Arg.Any<MasterProviderModel>())
               .Returns(new ProviderIndex());
 

            ResultsService resultsService = CreateResultsService(searchRepository: searchRepository, providerImportMappingService: mappingService);

            //Act
            IActionResult result = await resultsService.ImportProviders(request);

            //Assert
            await
                searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<ProviderIndex>>(m => m.Count() == 2));

            result
                .Should()
                .BeOfType<NoContentResult>();

            MasterProviderModel[] masterProviderModels = JsonConvert.DeserializeObject<MasterProviderModel[]>(json.ToString());

            masterProviderModels[0]
                .MasterDateOpened
                .Should()
                .NotBeNull();

            masterProviderModels[1]
               .MasterDateOpened
               .Should()
               .NotBeNull();
        }

        [TestMethod]
        public async Task RemoveCurrentProviders_WhenSummaryCountsExist_DeletesSummaryCounts()
        {
            //Arrange
            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<string>(Arg.Is(CacheKeys.AllProviderSummaryCount))
                .Returns(true);

            ResultsService resultsService = CreateResultsService(cacheProvider: cacheProvider);

            //Act
            IActionResult result = await resultsService.RemoveCurrentProviders();

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await
                cacheProvider
                .Received(1)
                .KeyDeleteAsync<string>(Arg.Is(CacheKeys.AllProviderSummaryCount));
        }

        [TestMethod]
        public async Task RemoveCurrentProviders_WhenSummariesExist_DeletesSummaries()
        {
            //Arrange
            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<List<ProviderSummary>>(Arg.Is(CacheKeys.AllProviderSummaries))
                .Returns(true);

            ResultsService resultsService = CreateResultsService(cacheProvider: cacheProvider);

            //Act
            IActionResult result = await resultsService.RemoveCurrentProviders();

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await
                cacheProvider
                .Received(1)
                .KeyDeleteAsync<List<ProviderSummary>>(Arg.Is(CacheKeys.AllProviderSummaries));
        }

        [TestMethod]
        public async Task RemoveCurrentProviders_WhenDeletingIndexThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            ICacheProvider cacheProvider = CreateCacheProvider();

            ISearchRepository<ProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(x => x.DeleteIndex())
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(cacheProvider: cacheProvider, searchRepository: searchRepository, logger: logger);

            //Act
            IActionResult result = await resultsService.RemoveCurrentProviders();

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to delete providers index"));

            await
                cacheProvider
                .DidNotReceive()
                .KeyExists<string>(Arg.Any<string>());

            await
                cacheProvider
                .DidNotReceive()
                .KeyExists<List<ProviderSummary>>(Arg.Any<string>());
        }

        static ResultsService CreateResultsService(ILogger logger = null,
            ICalculationResultsRepository resultsRepository = null,
            IMapper mapper = null,
            ISearchRepository<ProviderIndex> searchRepository = null,
            ITelemetry telemetry = null,
            IProviderSourceDatasetRepository providerSourceDatasetRepository = null,
            ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository = null,
            ISpecificationsRepository specificationsRepository = null,
            IResultsResilliencePolicies resiliencePolicies = null,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService = null,
            IPublishedProviderResultsRepository publishedProviderResultsRepository = null,
            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository = null,
            IProviderImportMappingService providerImportMappingService = null,
            ICacheProvider cacheProvider = null,
            ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationFeedSearchRepository = null,
            IProviderProfilingRepository providerProfilingRepository = null,
            IMessengerService messengerService = null)
        {
            return new ResultsService(
                logger ?? CreateLogger(),
                resultsRepository ?? CreateResultsRepository(),
                mapper ?? CreateMapper(),
                searchRepository ?? CreateSearchRepository(),
                telemetry ?? CreateTelemetry(),
                providerSourceDatasetRepository ?? CreateProviderSourceDatasetRepository(),
                calculationProviderResultsSearchRepository ?? CreateCalculationProviderResultsSearchRepository(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies(),
                publishedProviderResultsAssemblerService ?? CreateResultsAssembler(),
                publishedProviderResultsRepository ?? CreatePublishedProviderResultsRepository(),
                publishedProviderCalculationResultsRepository ?? CreatePublishedProviderCalculationResultsRepository(),
                providerImportMappingService ?? CreateProviderImportMappingService(),
                cacheProvider ?? CreateCacheProvider(),
                allocationNotificationFeedSearchRepository ?? CreateAllocationNotificationFeedSearchRepository(),
                providerProfilingRepository ?? CreateProfilingRepository(),
                messengerService ?? CreateMessengerService());
        }

        static IProviderProfilingRepository CreateProfilingRepository()
        {
            return Substitute.For<IProviderProfilingRepository>();
        }

        static ISearchRepository<AllocationNotificationFeedIndex> CreateAllocationNotificationFeedSearchRepository()
        {
            return Substitute.For<ISearchRepository<AllocationNotificationFeedIndex>>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IProviderImportMappingService CreateProviderImportMappingService()
        {
            return Substitute.For<IProviderImportMappingService>();
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

        static ISearchRepository<CalculationProviderResultsIndex> CreateCalculationProviderResultsSearchRepository()
        {
            return Substitute.For<ISearchRepository<CalculationProviderResultsIndex>>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static SpecificationCurrentVersion CreateSpecification(string specificationId)
        {
            return new SpecificationCurrentVersion
            {
                Id = specificationId,
                Policies = new[]
                {
                    new Policy
                    {
                        Id = "policy-1",
                        Name = "policy one",
                        Description = "test decscription",
                        Calculations = new[]
                        {
                            new Models.Specs.Calculation
                            {
                                Id = "calc-1"
                            },
                             new Models.Specs.Calculation
                            {
                                Id = "calc-2"
                            }
                        },
                        SubPolicies = new[]
                        {
                            new Policy
                            {
                                Id = "subpolicy-1",
                                Name = "sub policy one",
                                Description = "test decscription",
                                Calculations = new[]
                                {
                                    new Models.Specs.Calculation
                                    {
                                        Id = "calc-3"
                                    }

                                }
                            }
                        }
                    }
                }
            };
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
                            CalculationType = Models.Calcs.CalculationType.Funding
                        },
                        new CalculationResult
                        {
                            CalculationSpecification = new Reference { Id = "calc-spec-id-2", Name = "calc spec name 2"},
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = Models.Calcs.CalculationType.Number
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
                        LACode = "la code",
                        DateOpened = DateTime.Now.AddDays(-7)
                    }
                }
            };
        }

        static DocumentEntity<ProviderResult> CreateDocumentEntityWithNullCalculationResult()
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
                            Value = null,
                            CalculationType = Models.Calcs.CalculationType.Funding
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
                        LACode = "la code",
                        DateOpened = DateTime.Now.AddDays(-7)
                    }
                }
            };
        }

        static IEnumerable<MasterProviderModel> CreateProviderModels()
        {
            return new[]
            {
                new MasterProviderModel { MasterUKPRN = "1234" },
                new MasterProviderModel { MasterUKPRN = "5678" },
                new MasterProviderModel { MasterUKPRN = "1122" }
            };
        }

        static IEnumerable<PublishedProviderResult> CreatePublishedProviderResults()
        {
            return new[]
            {
                new PublishedProviderResult
                {
                    Title = "test title 1",
                    Summary = "test summary 1",
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new FundingStream
                        {
                            Id = "fs-1",
                            Name = "funding stream 1"
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new AllocationLine
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = FundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 50,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1"
                    }
                },
                new PublishedProviderResult
                {
                    Title = "test title 2",
                    Summary = "test summary 2",
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new FundingStream
                        {
                            Id = "fs-1",
                            Name = "funding stream 1"
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new AllocationLine
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = FundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1"
                    }
                },
                new PublishedProviderResult
                {
                    Title = "test title 3",
                    Summary = "test summary 3",
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new FundingStream
                        {
                            Id = "fs-2",
                            Name = "funding stream 2"
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new AllocationLine
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = FundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1"
                    }
                }
            };
        }

        static IEnumerable<PublishedProviderResult> CreatePublishedProviderResultsWithDifferentProviders()
        {
            return new[]
            {
                new PublishedProviderResult
                {
                    Title = "test title 1",
                    Summary = "test summary 1",
                    SpecificationId = "spec-1",
                    ProviderId = "1111",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new FundingStream
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new AllocationLine
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = FundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 50,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111",
                                    Name = "test provider name 1"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    Title = "test title 2",
                    Summary = "test summary 2",
                    SpecificationId = "spec-1",
                    ProviderId = "1111-1",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new FundingStream
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new AllocationLine
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = FundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111-1",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111-1",
                                    Name = "test provider name 2"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                },
                new PublishedProviderResult
                {
                    Title = "test title 3",
                    Summary = "test summary 3",
                    SpecificationId = "spec-1",
                    ProviderId = "1111-2",
                    FundingStreamResult = new PublishedFundingStreamResult
                    {
                        FundingStream = new FundingStream
                        {
                            Id = "fs-1",
                            Name = "funding stream 1",
                            ShortName = "fs1",
                            PeriodType = new PeriodType
                            {
                                Id = "pt1",
                                Name = "period-type 1",
                                StartDay = 1,
                                EndDay = 31,
                                StartMonth = 8,
                                EndMonth = 7
                            }
                        },
                        AllocationLineResult = new PublishedAllocationLineResult
                        {
                             AllocationLine = new AllocationLine
                            {
                                Id = "AAAAA",
                                Name = "test allocation line 1",
                                ShortName = "tal1",
                                FundingRoute = FundingRoute.LA,
                                IsContractRequired = true
                            },
                            Current = new PublishedAllocationLineResultVersion
                            {
                                Status = AllocationLineStatus.Held,
                                Value = 100,
                                Version = 1,
                                Date = DateTimeOffset.Now,
                                Provider = new ProviderSummary
                                {
                                    URN = "12345",
                                    UKPRN = "1111-2",
                                    UPIN = "2222",
                                    EstablishmentNumber = "es123",
                                    Authority = "London",
                                    ProviderType = "test type",
                                    ProviderSubType = "test sub type",
                                    DateOpened = DateTimeOffset.Now,
                                    ProviderProfileIdType = "UKPRN",
                                    LACode = "77777",
                                    Id = "1111-2",
                                    Name = "test provider name 3"
                                }
                            }
                        }
                    },
                    FundingPeriod = new Period
                    {
                        Id = "Ay12345",
                        Name = "fp-1",
                        StartDate = DateTimeOffset.Now,
                        EndDate = DateTimeOffset.Now.AddYears(1)
                    }
                }
            };
        }
    }
}
