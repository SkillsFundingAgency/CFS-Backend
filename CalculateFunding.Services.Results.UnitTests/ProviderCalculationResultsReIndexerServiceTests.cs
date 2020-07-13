using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class ProviderCalculationResultsReIndexerServiceTests
    {
        [TestMethod]
        public async Task ReIndexCalculationResults_GivenMessageWithUserDetails_LogsInitiated()
        {
            //Arrange
            Message message = new Message();
            message.UserProperties["user-id"] = "123";
            message.UserProperties["user-name"] = "Joe Bloggs";

            ILogger logger = CreateLogger();

            ProviderCalculationResultsReIndexerService service = CreateService(logger: logger);

            //Act
            await service.ReIndexCalculationResults(message);

            //Assert
            logger
               .Received(1)
               .Information($"{nameof(service.ReIndexCalculationResults)} initiated by: 'Joe Bloggs'");
        }

        [TestMethod]
        public void ReIndexCalculationResults_GivenResultReturnedFromDatabaseWithTwoCalcResultsButSearchReturnsErrors_ThrowsRetriableException()
        {
            //Arrange
            const string expectedErrorMessage = "Failed to index calculation provider result documents with errors: an error";

            Message message = new Message();
            message.UserProperties["user-id"] = "123";
            message.UserProperties["user-name"] = "Joe Bloggs";

            ProviderResult providerResult = CreateProviderResult();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<ProviderCalculationResultsIndex>>())
                .Returns(new[] { new IndexError { ErrorMessage = "an error" } });

            ICalculationResultsRepository calculationResultsRepository = CreateCalculationResultsRepository();

            calculationResultsRepository
                .WhenForAnyArgs(x => x.ProviderResultsBatchProcessing(default, default)).Do(x =>
                {
                    var y = x.Arg<Func<List<ProviderResult>, Task>>();
                    y(new List<ProviderResult> { providerResult }).GetAwaiter().GetResult();
                });

            ILogger logger = CreateLogger();


            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = providerResult.SpecificationId,
                Name = "spec name",
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, new List<SpecModel.SpecificationSummary> { specificationSummary }));

            ProviderCalculationResultsReIndexerService service = CreateService(
                resultsRepository: calculationResultsRepository,
                providerCalculationResultsSearchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                logger: logger);

            //Act
            Func<Task> test = async () => await service.ReIndexCalculationResults(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(expectedErrorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(expectedErrorMessage));

            logger
               .Received(1)
               .Information($"{nameof(service.ReIndexCalculationResults)} initiated by: 'Joe Bloggs'");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ReIndexCalculationResults_GivenResultReturnedFromDatabaseWithCalcResult_UpdatesSearch(bool featureToggleEnabled)
        {
            //Arrange
            Message message = new Message();

            ProviderResult providerResult = CreateProviderResult();

            ISearchRepository<ProviderCalculationResultsIndex> searchRepository = CreateSearchRepository();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = providerResult.SpecificationId,
                Name = "spec name",
            };

            ICalculationResultsRepository calculationResultsRepository = CreateCalculationResultsRepository();
            calculationResultsRepository
                .WhenForAnyArgs(x => x.ProviderResultsBatchProcessing(default, default)).Do(x =>
                {
                    var y = x.Arg<Func<List<ProviderResult>, Task>>();
                    y(new List<ProviderResult> { providerResult }).GetAwaiter().GetResult();
                });

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, new List<SpecModel.SpecificationSummary> { specificationSummary }));

            IFeatureToggle featureToggle = CreateFeatureToggle(featureToggleEnabled);

            ProviderCalculationResultsReIndexerService service = CreateService(
                resultsRepository: calculationResultsRepository,
                providerCalculationResultsSearchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                featureToggle: featureToggle);

            //Act
            await service.ReIndexCalculationResults(message);

            //Assert
            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(m => m.Count() == 1));

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<ProviderCalculationResultsIndex>>(
                        m =>
                            m.First().Id == "spec-id_prov-id" &&
                            m.First().SpecificationId == "spec-id" &&
                            m.First().SpecificationName == "spec name" &&
                            m.First().CalculationId.SequenceEqual(new[] { "calc-id-1", "calc-id-2" }) &&
                            m.First().CalculationName.SequenceEqual(new[] { "calc name 1", "calc name 2" }) &&
                            m.First().CalculationResult.SequenceEqual(new[] { "123", "10" }) &&
                            featureToggleEnabled ? m.First().CalculationException.SequenceEqual(new[] { "calc-id-1" }) : m.First().CalculationException == null &&
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
        public async Task ReIndexCalculationResults_GivenRequest_AddsServiceBusMessage()
        {
            //Arrange
            Reference user = new Reference("123", "Joe Bloggs");

            IMessengerService messengerService = CreateMessengerService();

            ProviderCalculationResultsReIndexerService service = CreateService(messengerService: messengerService);

            //Act
            IActionResult actionResult = await service.ReIndexCalculationResults(null, user);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
            messengerService
                .Received(1)
                .SendToQueue(
                    Arg.Is(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex),
                    Arg.Is(string.Empty),
                    Arg.Is<IDictionary<string, string>>(m => m["user-id"] == "123" && m["user-name"] == "Joe Bloggs"));
        }

        public static ProviderCalculationResultsReIndexerService CreateService(
            ILogger logger = null,
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            ICalculationResultsRepository resultsRepository = null,
            IFeatureToggle featureToggle = null,
            IMessengerService messengerService = null)
        {
            return new ProviderCalculationResultsReIndexerService(
                    logger ?? CreateLogger(),
                    providerCalculationResultsSearchRepository ?? CreateSearchRepository(),
                    specificationsApiClient ?? CreateSpecificationsApiClient(),
                    resultsRepository ?? CreateCalculationResultsRepository(),
                    CreateResiliencePolicies(),
                    featureToggle ?? CreateFeatureToggle(),
                    messengerService ?? CreateMessengerService()
                );
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ISearchRepository<ProviderCalculationResultsIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProviderCalculationResultsIndex>>();
        }

        static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private static ICalculationResultsRepository CreateCalculationResultsRepository()
        {
            return Substitute.For<ICalculationResultsRepository>();
        }

        private static IResultsResiliencePolicies CreateResiliencePolicies()
        {
            return ResultsResilienceTestHelper.GenerateTestPolicies();
        }

        private static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        private static IFeatureToggle CreateFeatureToggle(bool featureToggleEnabled = true)
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsExceptionMessagesEnabled()
                .Returns(featureToggleEnabled);

            return featureToggle;
        }

        static ProviderResult CreateProviderResult()
        {
            return new ProviderResult
            {
                CreatedAt = DateTime.Now,
                SpecificationId = "spec-id",
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult
                    {
                        Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                        Value = 123,
                        CalculationType = CalculationType.Template,
                        ExceptionType = "Exception"
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
    }
}
