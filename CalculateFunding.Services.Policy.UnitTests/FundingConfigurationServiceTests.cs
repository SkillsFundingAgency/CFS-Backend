using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Policy.UnitTests
{
    [TestClass]
    public class FundingConfigurationServiceTests
    {
        private const string fundingStreamId = "fs-1";

        private string fundingConfigurationsCacheKey = $"{CacheKeys.FundingConfig}{fundingStreamId}";

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

            FundingPeriod fundingPeriod = new FundingPeriod
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

            FundingPeriod fundingPeriod = new FundingPeriod
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

            FundingPeriod fundingPeriod = new FundingPeriod
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

        [TestMethod]
        public async Task GetFundingConfigurationsByFundingStreamId_GivenEmptyFundingStreamId_ReturnsBadRequestRequest()
        {
            // Arrange
            string fundingStreamId = string.Empty;

            ILogger logger = CreateLogger();

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(logger: logger);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfigurationsByFundingStreamId(fundingStreamId);

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
                .Error(Arg.Is("No funding stream Id was provided to GetFundingConfigurationsByFundingStreamId"));
        }

        [TestMethod]
        public async Task GetFundingConfigurationsByFundingStreamId_GivenFundingConfigurationsInCache_ReturnsFromCache()
        {
            // Arrange
            List<FundingConfiguration> fundingConfigs = new List<FundingConfiguration>
            {
                new FundingConfiguration()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<FundingConfiguration>>(Arg.Is(fundingConfigurationsCacheKey))
                .Returns(fundingConfigs);

            FundingConfigurationService fundingConfigurationsService = CreateFundingConfigurationService(cacheProvider: cacheProvider);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfigurationsByFundingStreamId(fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(fundingConfigs);
        }

        [TestMethod]
        public async Task GetFundingConfigurationsByFundingStreamId_GivenFundingConfigurationsNotInCacheAndNotInDatabase_ReturnsNotFound()
        {
            // Arrange
            List<FundingConfiguration> fundingConfigs = null;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<FundingConfiguration>>(Arg.Is(fundingConfigurationsCacheKey))
                .Returns(fundingConfigs);

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingConfigurationsByFundingStreamId(Arg.Is(fundingStreamId))
                .Returns(fundingConfigs);

            FundingConfigurationService fundingConfigurationsService =
                CreateFundingConfigurationService(cacheProvider: cacheProvider, policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfigurationsByFundingStreamId(fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFundingConfigurationsByFundingStreamId_GivenFundingConfigurationsNotInCacheBuInDatabase_ReturnsOKSetsInCache()
        {
            // Arrange
            List<FundingConfiguration> fundingConfigs = new List<FundingConfiguration>
            {
                new FundingConfiguration()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<FundingConfiguration>>(Arg.Is(fundingConfigurationsCacheKey))
                .Returns((List<FundingConfiguration>)null);

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingConfigurationsByFundingStreamId(Arg.Is(fundingStreamId))
                .Returns(fundingConfigs);

            FundingConfigurationService fundingConfigurationsService =
                CreateFundingConfigurationService(cacheProvider: cacheProvider, policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingConfigurationsService.GetFundingConfigurationsByFundingStreamId(fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(fundingConfigs);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Is(fundingConfigurationsCacheKey), Arg.Any<List<FundingConfiguration>>());
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
                        GroupTypeIdentifier = OrganisationGroupTypeIdentifier.LACode,
                        GroupTypeClassification =  OrganisationGroupTypeClassification.LegalEntity,
                        OrganisationGroupTypeCode = OrganisationGroupTypeCode.LocalAuthority,
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
