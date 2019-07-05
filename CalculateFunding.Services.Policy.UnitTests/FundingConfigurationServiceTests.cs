using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.FundingPolicy.ViewModels;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.UnitTests
{
    [TestClass]
    public class FundingConfigurationServiceTests
    {
        [TestMethod]
        [DataRow("1234", "5678")]
        public async Task GetFundingConfiguration_GivenFundingConfigurationDoesNotExist_ReturnsNotFoundRequest(string fundingStreamId, string fundingPeriodId)
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(logger: logger);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"No funding Configuration was found for funding stream id : {fundingStreamId} and funding period id : {fundingPeriodId}"));
        }

        [TestMethod]
        [DataRow("", "5678")]
        public async Task GetFundingConfiguration_GivenEmptyFundingStreamId_ReturnsBadRequestRequest(string fundingStreamId, string fundingPeriodId)
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(logger: logger);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding stream Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding stream Id was provided to GetFundingConfiguration"));
        }

        [TestMethod]
        [DataRow("1234", "")]
        public async Task GetFundingConfiguration_GivenEmptyFundingPeriodId_ReturnsBadRequestRequest(string fundingStreamId, string fundingPeriodId)
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(logger: logger);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding period Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding period Id was provided to GetFundingConfiguration"));
        }

        [TestMethod]
        [DataRow("1234", "5678")]
        public async Task GetFundingConfiguration__GivenFundingConfigurationWasFound_ReturnsSuccess(string fundingStreamId, string fundingPeriodId)
        {
            // Arrange
            FundingStream fundingStream = new FundingStream
            {
                Id = fundingStreamId
            };

            Period fundingPeriod = new Period
            {
                Id = fundingPeriodId
            };

            string configId = $"config-{fundingStreamId}-{fundingPeriodId}";

            FundingConfiguration fundingConfiguration = new FundingConfiguration
            {
                Id = configId
            };


            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            policyRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            policyRepository
                .GetFundingConfiguration(Arg.Is(configId))
                .Returns(fundingConfiguration);

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(fundingConfiguration);
        }

        [TestMethod]
        [DataRow("1234", "5678")]
        public async Task GetFundingConfiguration__GivenFundingConfigurationAlreadyInCache_ReturnsSuccessWithConfigurationFromCache(string fundingStreamId, string fundingPeriodId)
        {
            // Arrange
            FundingStream fundingStream = new FundingStream
            {
                Id = fundingStreamId
            };

            Period fundingPeriod = new Period
            {
                Id = fundingPeriodId
            };

            string configId = $"config-{fundingStreamId}-{fundingPeriodId}";

            FundingConfiguration fundingConfiguration = new FundingConfiguration
            {
                Id = configId
            };

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            policyRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            string cacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}-{fundingPeriodId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingConfiguration>(Arg.Is(cacheKey))
                .Returns(fundingConfiguration);

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(policyRepository: policyRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(fundingConfiguration);
        }

        [TestMethod]
        [DataRow("1234", "5678")]
        async public Task SaveFundingConfiguration_GivenValidConfigurationButFailedToSaveToDatabase_ReturnsStatusCode(string fundingStreamId, string fundingPeriodId)
        {
            //Arrange
            FundingStream fundingStream = new FundingStream
            {
                Id = fundingStreamId
            };

            Period fundingPeriod = new Period
            {
                Id = fundingPeriodId
            };

            ILogger logger = CreateLogger();

            HttpStatusCode statusCode = HttpStatusCode.BadRequest;

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            policyRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            policyRepository
                .SaveFundingConfiguration(Arg.Is<FundingConfiguration>(x => x.FundingStreamId == fundingStreamId && x.FundingPeriodId == fundingPeriodId))
                .Returns(statusCode);

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(logger: logger, policyRepository: policyRepository);

            FundingConfigurationViewModel fundingConfigurationViewModel = CreateConfigurationModel();

            //Act
            IActionResult result = await fundingConfigurationsService.SaveFundingConfiguration("Action", "Controller", fundingConfigurationViewModel, fundingStreamId, fundingPeriodId);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>();

            InternalServerErrorResult statusCodeResult = (InternalServerErrorResult)result;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save configuration file for funding stream id: {fundingStreamId} and period id: {fundingPeriodId} to cosmos db with status 400"));
        }

        private static FundingConfigurationService CreateFundingConfigurationService(
            ILogger logger = null,
            ICacheProvider cacheProvider = null,
            IMapper mapper = null,
            IPolicyRepository policyRepository = null,
            IValidator<FundingConfiguration> validator = null)
        {
            return new FundingConfigurationService(
                logger ?? CreateLogger(),
                cacheProvider ?? CreateCacheProvider(),
                mapper ?? CreateMapper(),
                policyRepository ?? CreatePolicyRepository(),
                PolicyResiliencePoliciesTestHelper.GenerateTestPolicies(),
                validator ?? CreateValidator());
        }

        private static FundingConfigurationViewModel CreateConfigurationModel()
        {
            return new FundingConfigurationViewModel
            {
                OrganisationGroupings = new[]
                {
                    new OrganisationGroupingConfiguration
                    {
                        GroupingReason = GroupingReason.Payment,
                        IdentifierType = OrganisationIdentifierType.LACode,
                        OrganisationGroupingType = OrganisationGroupingType.UKPRN,
                        ProviderTypeMatch = new[]
                        {
                            new ProviderTypeMatch
                            {
                                ProviderSubtype = "providerSubType",
                                ProviderType = "providerType"
                            }
                        }
                    }
                }
            };

        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<FundingConfigurationMappingProfile>();
            });

            return new Mapper(config);
        }

        private static IValidator<FundingConfiguration> CreateValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<FundingConfiguration> validator = Substitute.For<IValidator<FundingConfiguration>>();

            validator
               .ValidateAsync(Arg.Any<FundingConfiguration>())
               .Returns(validationResult);

            return validator;
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IPolicyRepository CreatePolicyRepository()
        {
            return Substitute.For<IPolicyRepository>();
        }
    }
}
