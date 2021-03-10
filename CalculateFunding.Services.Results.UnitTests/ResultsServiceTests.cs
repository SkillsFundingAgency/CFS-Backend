using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using CalcModel = CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Storage;
using Microsoft.Azure.Storage.Blob;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public partial class ResultsServiceTests
    {
        const string providerId = "123456";
        const string specificationId = "888999";
        const string jobId = "job-id";
        const string CalcsResultsContainerName = "calcresults";
        const string CalculationResultsReportFilePrefix = "calculation-results";

        [TestMethod]
        public async Task GetProviderResults_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderResults(null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderResults"));
        }

        [TestMethod]
        public async Task GetProviderResults_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderResults(providerId, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetProviderResults"));
        }

        [TestMethod]
        public async Task GetProviderResults_GivenNullProviderResultReturned_ReturnsNotFoundResult()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResult(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns((ProviderResult)null);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderResults(providerId, specificationId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A result was not found for provider id {providerId}, specification id {specificationId}"));
        }

        [TestMethod]
        public async Task GetProviderResults_GivenProviderResultReturned_ReturnsOK()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ProviderResult providerResult = CreateProviderResult();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResult(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns(providerResult);

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .GetCalculationMetadataForSpecification(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<CalcModel.CalculationMetadata>>(HttpStatusCode.OK, CreateCalculationMetadata()));

            ResultsService service = CreateResultsService(logger, resultsRepository, calculationsApiClient:calculationsApiClient);

            //Act
            IActionResult result = await service.GetProviderResults(providerId, specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetProviderSpecifications_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderSpecifications(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderSpecifications"));
        }

        [TestMethod]
        public async Task GetProviderResultsByCalculationType_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderResultByCalculationType(string.Empty, string.Empty, CalculationType.Template);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderResults"));
        }

        [TestMethod]
        public async Task GetProviderResultsByCalculationType_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetProviderResultByCalculationType(providerId, string.Empty, CalculationType.Template);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetProviderResults"));
        }

        [TestMethod]
        public async Task GetProviderResultsByCalculationType_GivenNullProviderResultReturned_ReturnsNotFoundResult()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResult(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns((ProviderResult)null);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Additional);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A result was not found for provider id {providerId}, specification id {specificationId}"));
        }

        [TestMethod]
        public async Task GetProviderResultsByCalculationType_GivenProviderResultReturned_ReturnsOK()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ProviderResult providerResult = new ProviderResult();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetProviderResultByCalculationType(Arg.Is(providerId), Arg.Is(specificationId), Arg.Is(CalculationType.Additional))
                .Returns(providerResult);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Additional);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetProviderSpecifications_GivenEmptyProviderResultsReturned_ReturnsOKWithEmptyCollection()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IEnumerable<ProviderResult> providerResults = Enumerable.Empty<ProviderResult>();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSpecificationResults(Arg.Is(providerId))
                .Returns(providerResults);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetProviderSpecifications(providerId);

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
        public async Task GetProviderSpecifications_GivenProviderResultsReturned_ReturnsOK()
        {
            //Arrange
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
            IActionResult result = await service.GetProviderSpecifications(providerId);

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
        public async Task GetProviderSpecifications_GivenProviderResultsWithDuplicateSummariesReturned_ReturnsOK()
        {
            //Arrange
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
            IActionResult result = await service.GetProviderSpecifications(providerId);

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
        public async Task GetProviderResultsBySpecificationId_GivenNoSpecificationIsProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger);

            //Act
            IActionResult result = await service.GetProviderResultsBySpecificationId(null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task GetProviderResultsBySpecificationId_GivenSpecificationIsProvided_ReturnsResults()
        {
            //Arrange
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
            IActionResult result = await service.GetProviderResultsBySpecificationId(specificationId, null);

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
        public async Task GetProviderResultsBySpecificationId_GivenSpecificationIsProvidedAndTopIsProvided_ReturnsResults()
        {
            //Arrange
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
            IActionResult result = await service.GetProviderResultsBySpecificationId(specificationId, "1");

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
        public async Task ProviderHasResultsBySpecificationId_GivenNoSpecificationIsProvided_ReturnsBadRequest()
        {
            //Arrange
            ResultsService service = CreateResultsService();

            //Act
            IActionResult result = await service.ProviderHasResultsBySpecificationId(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ProviderHasResultsBySpecificationId_GivenSpecificationIsProvided_ReturnsResults(bool hasResults)
        {
            //Arrange
            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .ProviderHasResultsBySpecificationId(specificationId)
                .Returns(Task.FromResult(hasResults));

            ResultsService service = CreateResultsService(resultsRepository: resultsRepository);

            //Act
            IActionResult result = await service.ProviderHasResultsBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResults = result as OkObjectResult;

            bool okResultsValue = (bool) okResults.Value;

            okResultsValue
                .Should()
                .Be(hasResults);
        }

        [TestMethod]
        public async Task GetProviderSourceDatasetsByProviderIdAndSpecificationId_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger);

            //Act
            IActionResult result = await service.GetProviderSourceDatasetsByProviderIdAndSpecificationId(null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetProviderResultsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetProviderSourceDatasetsByProviderIdAndSpecificationId_GivenNullOrEmptyProviderId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger);

            //Act
            IActionResult result = await service.GetProviderSourceDatasetsByProviderIdAndSpecificationId(specificationId, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No provider Id was provided to GetProviderResultsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetProviderSourceDatasetsByProviderIdAndSpecificationId_GivenResultsReturned_ReturnsOKResult()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IEnumerable<ProviderSourceDataset> providerSources = new[] { new ProviderSourceDataset(), new ProviderSourceDataset() };

            IProviderSourceDatasetRepository providerSourceDatasetRepository = CreateProviderSourceDatasetRepository();
            providerSourceDatasetRepository
                .GetProviderSourceDatasets(Arg.Is(providerId), Arg.Is(specificationId))
                .Returns(providerSources);

            ResultsService service = CreateResultsService(logger, providerSourceDatasetRepository: providerSourceDatasetRepository);

            //Act
            IActionResult result = await service.GetProviderSourceDatasetsByProviderIdAndSpecificationId(specificationId, providerId);

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

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<ProviderCalculationResultsIndex>>())
                .Returns(new[] { new IndexError { ErrorMessage = "an error" } });

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = providerResult.Content.SpecificationId,
                Name = "spec name",
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationSummary.Id))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            ResultsService resultsService = CreateResultsService(
                resultsRepository: calculationResultsRepository,
                calculationProviderResultsSearchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                logger: logger);

            //Act
            IActionResult actionResult = await resultsService.ReIndexCalculationProviderResults();

            //Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>();

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

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = providerResult.Content.SpecificationId,
                Name = "spec name",
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationSummary.Id))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            ResultsService resultsService = CreateResultsService(
                resultsRepository: calculationResultsRepository,
                calculationProviderResultsSearchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult actionResult = await resultsService.ReIndexCalculationProviderResults();

            //Assert
            actionResult
                .Should()
                .BeOfType<NoContentResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(m => m.Count() == 2));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(
                        m =>
                            m.First().SpecificationId == "spec-id" &&
                            m.First().SpecificationName == "spec name" &&
                            m.First().CalculationId.First() == "calc-id-1" &&
                            m.First().CalculationName.First() == "calc name 1" &&
                            m.First().CalculationResult.First() == "123" &&
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
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(
                        m =>
                            m.Last().SpecificationId == "spec-id" &&
                            m.Last().SpecificationName == "spec name" &&
                            m.Last().CalculationId.Last() == "calc-id-2" &&
                            m.Last().CalculationName.Last() == "calc name 2" &&
                            m.Last().CalculationResult.Last() == "10" &&
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

        [TestMethod]
        public async Task ReIndexCalculationProviderResults_GivenResultReturnedFromDatabaseWithCalcResultWithNullValue_UpdatesSearch_AndSetsIsExcluded_ThenReturnsNoContent()
        {
            //Arrange
            DocumentEntity<ProviderResult> providerResult = CreateDocumentEntityWithNullCalculationResult();

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .GetAllProviderResults()
                .Returns(new[] { providerResult });

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateCalculationProviderResultsSearchRepository();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = providerResult.Content.SpecificationId,
                Name = "spec name",
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(specificationSummary.Id))
                .Returns(new ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            ResultsService resultsService = CreateResultsService(
                resultsRepository: calculationResultsRepository,
                calculationProviderResultsSearchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult actionResult = await resultsService.ReIndexCalculationProviderResults();

            //Assert
            actionResult
                .Should()
                .BeOfType<NoContentResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(m => m.Count() == 1));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(
                        m =>
                            m.First().SpecificationId == "spec-id" &&
                            m.First().SpecificationName == "spec name" &&
                            m.First().CalculationId.First() == "calc-id-1" &&
                            m.First().CalculationName.First() == "calc name 1" &&
                            m.First().CalculationResult.First() == "null" &&
                            m.First().ProviderId == "prov-id" &&
                            m.First().ProviderName == "prov name" &&
                            m.First().ProviderType == "prov type" &&
                            m.First().ProviderSubType == "prov sub type" &&
                            m.First().UKPRN == "ukprn" &&
                            m.First().UPIN == "upin" &&
                            m.First().URN == "urn" &&
                            m.First().EstablishmentNumber == "12345"
                    ));
        }

        [TestMethod]
        public async Task CleanupProviderResultsForSpecification_GivenProviderResultsBySpecificationIdAndProviders_ThenCallsDelete()
        {
            //Arrange
            DocumentEntity<ProviderResult> providerResult = CreateDocumentEntityWithNullCalculationResult();

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .GetProviderResultsBySpecificationIdAndProviders(Arg.Any<IEnumerable<string>>(), Arg.Is<string>(specificationId))
                .Returns(new ProviderResult[] { providerResult.Content });

            ResultsService resultsService = CreateResultsService(
                resultsRepository: calculationResultsRepository);

            SpecificationProviders specificationProviders = new SpecificationProviders { SpecificationId = providerResult.Content.SpecificationId, Providers = new string[] { providerResult.Content.Id } };

            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                { "specificationId", specificationId },
                { "sfa-correlationId", Guid.NewGuid().ToString() }
            };

            Message message = new Message { Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(specificationProviders)) };

            message.UserProperties["specificationId"] = specificationId;

            //Act
            await resultsService.Process(message);

            //Assert
            await calculationResultsRepository
                .Received(1)
                .DeleteCurrentProviderResults(Arg.Is<IEnumerable<ProviderResult>>(x => x.First().Provider.Id == providerResult.Content.Provider.Id));
        }

        [TestMethod]
        public async Task HasCalculationResults_GivenCalculationNotFound_ReturnNotFoundResult()
        {
            //Arrange
            const string calculationId = "calc-1";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(calculationId))
                .Returns((Common.ApiClient.Calcs.Models.Calculation)null);

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await resultsService.HasCalculationResults(calculationId);

            //Assert
            actionResult
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Calculation could not be found for calculation id '{calculationId}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Calculation could not be found for calculation id '{calculationId}'"));
        }

        [TestMethod]
        public async Task HasCalculationResults_GivenProviderResultNotFoundForSpecification_ReturnHasCalculationResultsFalse()
        {
            //Arrange
            const string calculationId = "calc-1";
            const string specificationId = "spec-1";

            Common.ApiClient.Calcs.Models.Calculation calculation = new Common.ApiClient.Calcs.Models.Calculation
            {
                Id = calculationId,
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(calculationId))
                .Returns(calculation);

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSingleProviderResultBySpecificationId(Arg.Is(specificationId))
                .Returns((ProviderResult)null);

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository, calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await resultsService.HasCalculationResults(calculationId);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(false);
        }

        [TestMethod]
        public async Task HasCalculationResults_GivenProviderResultFoundForSpecificationButNoCalculations_ReturnHasCalculationResultsFalse()
        {
            //Arrange
            const string calculationId = "calc-1";
            const string specificationId = "spec-1";

            Common.ApiClient.Calcs.Models.Calculation calculation = new Common.ApiClient.Calcs.Models.Calculation
            {
                Id = calculationId,
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(calculationId))
                .Returns(calculation);

            ProviderResult providerResult = new ProviderResult();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSingleProviderResultBySpecificationId(Arg.Is(specificationId))
                .Returns(providerResult);

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository, calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await resultsService.HasCalculationResults(calculationId);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(false);
        }

        [TestMethod]
        public async Task HasCalculationResults_GivenProviderResultFoundForSpecificationButNoMatchingCalculations_ReturnHasCalculationResultsFalse()
        {
            //Arrange
            const string calculationId = "calc-1";
            const string specificationId = "spec-1";

            Common.ApiClient.Calcs.Models.Calculation calculation = new Common.ApiClient.Calcs.Models.Calculation
            {
                Id = calculationId,
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(calculationId))
                .Returns(calculation);

            ProviderResult providerResult = new ProviderResult
            {
                SpecificationId = specificationId,
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult
                    {
                        Calculation = new Reference
                        {
                            Id = "calc-2"
                        }
                    }
                }
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSingleProviderResultBySpecificationId(Arg.Is(specificationId))
                .Returns(providerResult);

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository, calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await resultsService.HasCalculationResults(calculationId);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(false);
        }

        [TestMethod]
        public async Task HasCalculationResults_GivenProviderResultFoundForSpecificationWithMatchingCalculation_ReturnHasCalculationResultsTrue()
        {
            //Arrange
            const string calculationId = "calc-1";
            const string specificationId = "spec-1";

            Common.ApiClient.Calcs.Models.Calculation calculation = new Common.ApiClient.Calcs.Models.Calculation
            {
                Id = calculationId,
                SpecificationId = specificationId
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(calculationId))
                .Returns(calculation);

            ProviderResult providerResult = new ProviderResult
            {
                SpecificationId = specificationId,
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult
                    {
                        Calculation = new Reference
                        {
                            Id = "calc-1"
                        }
                    }
                }
            };

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSingleProviderResultBySpecificationId(Arg.Is(specificationId))
                .Returns(providerResult);

            ResultsService resultsService = CreateResultsService(resultsRepository: resultsRepository, calculationsRepository: calculationsRepository);

            //Act
            IActionResult actionResult = await resultsService.HasCalculationResults(calculationId);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(true);
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 0)]
        public async Task QueueCsvGenerationMessage_RunsAsExpected(bool hasResults, int expectedOperations)
        {
            //Arrange
            string specificationId = "12345";
            string specificationName = "67890";

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTime(specificationId, Arg.Any<DateTimeOffset>())
                .Returns(hasResults);

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .DoesBlobExistAsync($"{CalculationResultsReportFilePrefix}-{specificationId}", CalcsResultsContainerName)
                .Returns(true);

            ResultsService resultsService = CreateResultsService(logger: logger,
                resultsRepository: calculationResultsRepository,
                jobManagement: jobManagement,
                blobClient: blobClient);

            //Act
            await resultsService.QueueCsvGenerationMessageIfNewCalculationResults(specificationId, specificationName);

            //Assert

            logger
                .Received(expectedOperations)
                .Information($"Found new calculation results for specification id '{specificationId}'");

            await jobManagement
                .Received(expectedOperations)
                .QueueJob(
                    Arg.Is<JobCreateModel>(_ => 
                    _.JobDefinitionId == JobConstants.DefinitionNames.GenerateCalcCsvResultsJob && 
                    _.SpecificationId == specificationId && 
                    _.Properties["specification-id"] == specificationId && _.Properties["specification-name"] == specificationName));
        }

        [TestMethod]
        public async Task QueueCsvGeneration_RunsAsExpected()
        {
            //Arrange
            string specificationId = "12345";
            string specificationName = "67890";
            string jobId = "12345678";

            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, Name = specificationName };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(specificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTime(specificationId, Arg.Any<DateTimeOffset>())
                .Returns(true);

            ILogger logger = CreateLogger();

            Job job = new Job { Id = jobId };

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Is<JobCreateModel>(_ =>
                    _.JobDefinitionId == JobConstants.DefinitionNames.GenerateCalcCsvResultsJob &&
                    _.SpecificationId == specificationId &&
                    _.Properties["specification-id"] == specificationId && _.Properties["specification-name"] == specificationName))
                .Returns(job);

            ResultsService resultsService = CreateResultsService(logger: logger,
                resultsRepository: calculationResultsRepository,
                jobManagement: jobManagement,
                specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await resultsService.QueueCsvGeneration(specificationId);

            //Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>();
            OkObjectResult okObjectResult = result as OkObjectResult;
            okObjectResult.Should().NotBeNull();
            okObjectResult.Value.Should().NotBeNull();
            okObjectResult.Value.Should().BeOfType<Job>();
            Job actualJob = okObjectResult.Value as Job;
            actualJob.Id.Should().Be(jobId);
        }

        [TestMethod]
        [DataRow("SpecId1", DeletionType.SoftDelete)]
        [DataRow("SpecId1", DeletionType.PermanentDelete)]
        public async Task DeleteCalculationResults_Deletes_Dependencies_Using_Correct_SpecificationId_And_DeletionType(string specificationId, DeletionType deletionType)
        {
            Message message = new Message
            {
                UserProperties =
                {
                    new KeyValuePair<string, object>("jobId", jobId),
                    new KeyValuePair<string, object>("specification-id", specificationId),
                    new KeyValuePair<string, object>("deletion-type", (int)deletionType)
                }
            };
            ICalculationResultsRepository calculationsRepository = CreateResultsRepository();
            ResultsService resultsService = CreateResultsService(resultsRepository: calculationsRepository);

            await resultsService.DeleteCalculationResults(message);

            await calculationsRepository.Received(1).DeleteCalculationResultsBySpecificationId(specificationId, deletionType);
        }

        [TestMethod]
        public async Task GetSpecificationCalculationResultsMetadata_GivenNullOrEmptySpecificationId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ResultsService service = CreateResultsService(logger: logger);

            //Act
            IActionResult result = await service.GetSpecificationCalculationResultsMetadata(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to get calculation results metadata"));
        }

        [TestMethod]
        public async Task GetSpecificationCalculationResultsMetadata_GivenCalculationResultsMetadataReturned_ReturnsOK()
        {
            //Arrange
            ILogger logger = CreateLogger();
            DateTime lastUpdated = new RandomDateTime();

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSpecificationCalculationResultsLastUpdated(Arg.Is(specificationId))
                .Returns(lastUpdated);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetSpecificationCalculationResultsMetadata(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<SpecificationCalculationResultsMetadata>()
                .Which
                .LastUpdated
                .Should()
                .Be(lastUpdated);
        }

        [TestMethod]
        public async Task GetSpecificationCalculationResultsMetadata_GivenNoCalculationResultsMetadataReturned_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();
            DateTime? lastUpdated = null;

            ICalculationResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .GetSpecificationCalculationResultsLastUpdated(Arg.Is(specificationId))
                .Returns(lastUpdated);

            ResultsService service = CreateResultsService(logger, resultsRepository);

            //Act
            IActionResult result = await service.GetSpecificationCalculationResultsMetadata(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        #region "Dependency creation"
        static ResultsService CreateResultsService(
            ILogger logger = null,
            ICalculationResultsRepository resultsRepository = null,
            IProviderSourceDatasetRepository providerSourceDatasetRepository = null,
            ISearchRepository<ProviderCalculationResultsIndex> calculationProviderResultsSearchRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            ICalculationsApiClient calculationsApiClient = null,
            IResultsResiliencePolicies resiliencePolicies = null,
            ICalculationsRepository calculationsRepository = null,
            IBlobClient blobClient = null,
            IJobManagement jobManagement = null)
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle.IsExceptionMessagesEnabled().Returns(true);
            return new ResultsService(
                logger ?? CreateLogger(),
                featureToggle,
                resultsRepository ?? CreateResultsRepository(),
                providerSourceDatasetRepository ?? CreateProviderSourceDatasetRepository(),
                calculationProviderResultsSearchRepository ?? CreateCalculationProviderResultsSearchRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                calculationsApiClient ?? CreateCalculationsApiClient(),
                resiliencePolicies ?? ResultsResilienceTestHelper.GenerateTestPolicies(),
                calculationsRepository ?? CreateCalculationsRepository(),
                CreateMapper(),
                jobManagement ?? CreateJobManagement(),
                blobClient ?? CreateBlobClient());
        }

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
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


        static ISearchRepository<ProviderCalculationResultsIndex> CreateCalculationProviderResultsSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderCalculationResultsIndex>>();
        }

        static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        static ICalculationsApiClient CreateCalculationsApiClient()
        {
            return Substitute.For<ICalculationsApiClient>();
        }

        static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<ResultsMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
        #endregion "Dependency creation"

        #region "Test data"

        static IEnumerable<CalcModel.CalculationMetadata> CreateCalculationMetadata()
        {
            return new[]
            {
                new CalcModel.CalculationMetadata { CalculationId = "calc-id-1", ValueType = CalcModel.CalculationValueType.Number },
                new CalcModel.CalculationMetadata { CalculationId = "calc-id-2", ValueType = CalcModel.CalculationValueType.Currency }
            };
        }

        static ProviderResult CreateProviderResult()
        {
            return new ProviderResult
            {
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = 123,
                            CalculationType = CalculationType.Template
                        },
                        new CalculationResult
                        {
                            Calculation = new Reference { Id = "calc-id-2", Name = "calc name 2" },
                            Value = 10,
                            CalculationType = CalculationType.Template
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
            };
        }

        static DocumentEntity<ProviderResult> CreateDocumentEntity()
        {
            return new DocumentEntity<ProviderResult>
            {
                UpdatedAt = DateTime.Now,
                Content = CreateProviderResult()
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
                            Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                            Value = null,
                            CalculationType = CalculationType.Template
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

        #endregion "Test data"
    }
}
