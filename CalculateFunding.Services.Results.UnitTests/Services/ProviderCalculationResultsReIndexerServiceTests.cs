using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.UnitTests;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Services
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
        public async Task ReIndexCalculationResults_GivenRequest_AddsServiceBusMessage()
        {
            //Arrange
             ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, "123"), new Claim(ClaimTypes.Name, "Joe Bloggs") })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .HttpContext
                .Returns(context);

            IMessengerService messengerService = CreateMessengerService();

            ProviderCalculationResultsReIndexerService service = CreateService(messengerService: messengerService);

            //Act
            IActionResult actionResult = await service.ReIndexCalculationResults(request);

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
            ISpecificationsRepository specificationsRepository = null,
            ICalculationResultsRepository resultsRepository = null,
            IFeatureToggle featureToggle = null,
            IMessengerService messengerService = null)
        {
            return new ProviderCalculationResultsReIndexerService(
                    logger ?? CreateLogger(),
                    providerCalculationResultsSearchRepository ?? CreateSearchRepository(),
                    specificationsRepository ?? CreateSpecificationsRepository(),
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

        private static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
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
                        CalculationSpecification = new Reference { Id = "calc-spec-id-1", Name = "calc spec name 1"},
                        Calculation = new Reference { Id = "calc-id-1", Name = "calc name 1" },
                        Value = 123,
                        CalculationType = Models.Calcs.CalculationType.Funding,
                        ExceptionType = "Exception"
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
            };
        }
    }
}
