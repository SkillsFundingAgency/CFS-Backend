using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class ProfilingServiceTests
    {
        protected IProfilingApiClient profilingClient1 { get; private set; }

        [TestMethod]
        public void SaveFundingTotals_GivenNullPublishedProfilingFundlingTotalsRequests_ThrowsArgumentException()
        {
            //Arrange
            List<FundingLine> fundingLines = new List<FundingLine>();

            ProfilingService service = CreateProfilingService();

            //Act
            Func<Task> test = async () => await service.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SaveFundingTotals_GivenPublishedProfilingRequestsButProfilingAPICausesException_LogsAndThrows()
        {
            //Arrange
            IEnumerable<FundingLine> fundingLines = SetUpInput();

            ILogger logger = CreateLogger();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            ValidatedApiResponse<ProviderProfilingResponseModel> providerProfilingResponseModel =
                new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.InternalServerError);

            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(providerProfilingResponseModel);

            ProfilingService service = CreateProfilingService(
                logger: logger,
                profilingApiClient: providerProfilingRepository);

            //Act
            Func<Task> test = async () => await service.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Failed to Get Profile Periods for updating for Requested FundingPeriodId: 'AY-1819' and FundingStreamId: 'PSG'");

            logger
               .Received(1)
               .Error(Arg.Is("Failed to Get Profile Periods for updating for Requested FundingPeriodId: 'AY-1819' and FundingStreamId: 'PSG'"));
        }

        [TestMethod]
        public void SaveFundingTotals_GivenPublishedProfilingRequestsButNoPaymentTypeinFundingTotals_LogsAndThrows()
        {
            //Arrange
            List<FundingLine> fundingLines = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = FundingLineType.Information,Value = 500, TemplateLineId = 123, DistributionPeriods = null}

            };

            ILogger logger = CreateLogger();

            ProfilingService service = CreateProfilingService(logger: logger);

            //Act
            Func<Task> test = async () => await service.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert

            logger
               .Received(0)
               .Error(Arg.Is("No Funding Values of Type Payment in the Funding Totals for updating."));
        }

        [TestMethod]
        public void SaveFundingTotals_GivenPublishedProfilingRequestsButProfilingAPIReturnNoResults_LogsAndThrows()
        {
            //Arrange
            IEnumerable<FundingLine> fundingLines = SetUpInput();

            ILogger logger = CreateLogger();

            SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ValidatedApiResponse<ProviderProfilingResponseModel>>(null));

            ProfilingService serviceapi = CreateProfilingService(
                logger: logger,
                profilingApiClient: providerProfilingRepository);

            //Act
            Func<Task> test = async () => await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert
            logger
                .Received(0)
                .Error(Arg.Is("Failed to Get Profile Periods for updating  for Requested FundingPeriodId"));
        }


        [TestMethod]
        public async Task SaveFundingTotals_GivenPublishedProfilingRequests()
        {
            //Arrange
            IEnumerable<FundingLine> fundingLines = SetUpInput();

            ILogger logger = CreateLogger();

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult(profileResponse));

            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert 
            fundingLines.Where(y => y.Type == FundingLineType.Payment)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            IEnumerable<FundingLine> expectedFundingLines;
            expectedFundingLines = ExpectedOutput();
            JsonConvert
                .SerializeObject(expectedFundingLines)
                .Should()
                .BeEquivalentTo(JsonConvert.SerializeObject(fundingLines));

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.FundingValue == 500));
        }

        [TestMethod]
        public async Task SaveMultipleFundinglines_GivenPublishedProfilingRequests()
        {
            //Arrange
            List<FundingLine> fundingLines = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = FundingLineType.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null},
                new FundingLine { Name="Xyz",FundingLineCode = "AB1", Type = FundingLineType.Payment,Value = 600, TemplateLineId = 123, DistributionPeriods = null}
            };

            ILogger logger = CreateLogger();
            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult(profileResponse));

            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert            
            fundingLines.Where(y => y.Value == 500)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            fundingLines.Where(y => y.Value == 600)
               .Select(r => r.DistributionPeriods)
               .Should()
               .NotBeNullOrEmpty();

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.FundingValue == 500));

            await providerProfilingRepository
               .Received(1)
               .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                   m.FundingValue == 600));
        }

        [TestMethod]
        public async Task SaveMultipleSameFundinglines_GivenPublishedProfilingRequests()
        {
            //Arrange
            List<FundingLine> fundingLines = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = FundingLineType.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null},
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = FundingLineType.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null}
            };

            ILogger logger = CreateLogger();
            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult(profileResponse));

            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert  
            fundingLines
                .Where(y => y.Value == 500)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.FundingValue == 500));
        }

        [TestMethod] 
        public async Task SaveFundingTotals_GivenPublishedProfilingRequestsReturnProfilePatternKey()
        {
            //Arrange
            IEnumerable<FundingLine> fundingLines = SetUpInput();
            IEnumerable<ProfilePatternKey> profilePatternKeys = SetUpProfilePatternKeyt();

            ILogger logger = CreateLogger();

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult(profileResponse));

            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            IEnumerable<ProfilePatternKey> result = await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819", 
                profilePatternKeys, "productType", "productSubType");

            //Assert 
            fundingLines.Where(y => y.Type == FundingLineType.Payment)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            IEnumerable<FundingLine> expectedFundingLines;
            expectedFundingLines = ExpectedOutput();
            JsonConvert
                .SerializeObject(expectedFundingLines)
                .Should()
                .BeEquivalentTo(JsonConvert.SerializeObject(fundingLines));

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.FundingValue == 500));

            result.Select(m => m.Key)
                .Should()
                .BeEquivalentTo("ProfilePatthernKey1");
        }

        private static IEnumerable<FundingLine> SetUpInput()
        {
            List<FundingLine> fundingLines = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = FundingLineType.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null}
            };

            return fundingLines;
        }

        private static IEnumerable<ProfilePatternKey> SetUpProfilePatternKeyt()
        {
            List<ProfilePatternKey> profilePatternKey = new List<ProfilePatternKey>
            {
                new ProfilePatternKey { Key = "key1"}
            };

            return profilePatternKey;
        }

        private static IEnumerable<FundingLine> ExpectedOutput()
        {
            List<DistributionPeriod> distributionPeriod = new List<DistributionPeriod>();

            List<ProfilePeriod> profiles = new List<ProfilePeriod>
            {
                 new ProfilePeriod { DistributionPeriodId = "2018-2019", Occurrence = 1, Type = ProfilePeriodType.CalendarMonth, TypeValue = "October", ProfiledValue = 82190.0M, Year = 2018},
                 new ProfilePeriod { DistributionPeriodId = "2018-2019", Occurrence = 1, Type = ProfilePeriodType.CalendarMonth, TypeValue = "April", ProfiledValue = 82190.0M, Year = 2019}
            };

            distributionPeriod.Add(new DistributionPeriod()
            {
                ProfilePeriods = profiles,
                DistributionPeriodId = "2018-2019",
                Value = 82190.0M
            });

            List<FundingLine> fundingLines = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = FundingLineType.Payment,Value = 500, TemplateLineId = 123,
                    DistributionPeriods = distributionPeriod}
            };

            return fundingLines;
        }
        private static ValidatedApiResponse<ProviderProfilingResponseModel> SetUpProviderProfilingResponse()
        {
            return new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel()
            {
                DeliveryProfilePeriods = new List<ProfilingPeriod>
                 {
                    new ProfilingPeriod { Period = "October", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new ProfilingPeriod { Period = "April", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 },
                DistributionPeriods = new List<DistributionPeriods>
                 {
                    new DistributionPeriods { DistributionPeriodCode = "2018-2019",   Value = 82190.0M }
                 },
                ProfilePatternKey = "ProfilePatthernKey1"

            });
        }

        protected void GivenTheApiResponseDetailsForTheProfileRequest(ProviderProfilingResponseModel providerProfilingResponseModel,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            profilingClient1 = CreateProfilingRepository();
            profilingClient1.GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(new ValidatedApiResponse<ProviderProfilingResponseModel>(statusCode,
                    providerProfilingResponseModel));
        }

        static ProfilingService CreateProfilingService(ILogger logger = null,
            IProfilingApiClient profilingApiClient = null)
        {
            return new ProfilingService(
                logger ?? CreateLogger(),
                profilingApiClient ?? CreateProfilingRepository(),
                GenerateTestPolicies());
        }

        public static IPublishingResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                ProfilingApiClient = Policy.NoOpAsync(),
            };
        }

        static IProfilingApiClient CreateProfilingRepository()
        {
            return Substitute.For<IProfilingApiClient>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
