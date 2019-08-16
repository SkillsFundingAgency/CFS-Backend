using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.UnitTests;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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
            List<FundingLine> fundingLine = new List<FundingLine>();

            Dictionary<string, IEnumerable<FundingLine>> fundingLines = new Dictionary<string, IEnumerable<FundingLine>>();

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
            Dictionary<string, IEnumerable<FundingLine>> fundingLines = SetUpInput();

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
            List<FundingLine> fundingLine = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Information,Value = 500, TemplateLineId = 123, DistributionPeriods = null}

            };

            Dictionary<string, IEnumerable<FundingLine>> fundingLines = new Dictionary<string, IEnumerable<FundingLine>>
            {
                { "test", fundingLine }

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
            Dictionary<string, IEnumerable<FundingLine>> fundingLines = SetUpInput();

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
        public async Task  SaveFundingTotals_GivenPublishedProfilingRequests()
        {
            //Arrange
            Dictionary<string, IEnumerable<FundingLine>> fundingLines = SetUpInput();

            ILogger logger = CreateLogger();

            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse =  SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ValidatedApiResponse<ProviderProfilingResponseModel>>(profileResponse));

            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert 
            fundingLines.Values
                .SelectMany(x => x)
                .Where(y => y.Type == OrganisationGroupingReason.Payment)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            Dictionary<string, IEnumerable<FundingLine>> expectedFudingLines;
            expectedFudingLines = ExpectedOutput();
            JsonConvert.SerializeObject(expectedFudingLines).Should().BeEquivalentTo(JsonConvert.SerializeObject(fundingLines));

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.FundingValue == 500));
        }

        [TestMethod]
        public async Task SaveMultipleFundinglines_GivenPublishedProfilingRequests()
        {
            //Arrange
            List<FundingLine> fundingLine = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null},
                new FundingLine { Name="Xyz",FundingLineCode = "AB1", Type = OrganisationGroupingReason.Payment,Value = 600, TemplateLineId = 123, DistributionPeriods = null}

            };

            Dictionary<string, IEnumerable<FundingLine>> fundingLines = new Dictionary<string, IEnumerable<FundingLine>>
            {
                { "test", fundingLine }

            };

            ILogger logger = CreateLogger();
            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ValidatedApiResponse<ProviderProfilingResponseModel>>(profileResponse));

            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert            
            fundingLines.Values
                .SelectMany(x => x)
                .Where(y => y.Value == 500)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            fundingLines.Values
               .SelectMany(x => x)
               .Where(y => y.Value == 600)
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
            List<FundingLine> fundingLine = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null},
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null}

            };

            Dictionary<string, IEnumerable<FundingLine>> fundingLines = new Dictionary<string, IEnumerable<FundingLine>>
            {
                { "test", fundingLine }

            };

            ILogger logger = CreateLogger();
            ValidatedApiResponse<ProviderProfilingResponseModel> profileResponse = SetUpProviderProfilingResponse();

            IProfilingApiClient providerProfilingRepository = Substitute.For<IProfilingApiClient>();
            providerProfilingRepository
                .GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(Task.FromResult<ValidatedApiResponse<ProviderProfilingResponseModel>>(profileResponse));
           
            ProfilingService serviceapi = CreateProfilingService(
            logger: logger,
               profilingApiClient: providerProfilingRepository);

            //Act
            await serviceapi.ProfileFundingLines(fundingLines, "PSG", "AY-1819");

            //Assert  
            fundingLines.Values
                .SelectMany(x => x)
                .Where(y => y.Value == 500)
                .Select(r => r.DistributionPeriods)
                .Should()
                .NotBeNullOrEmpty();

            await providerProfilingRepository
                .Received(1)
                .GetProviderProfilePeriods(Arg.Is<ProviderProfilingRequestModel>(m =>
                    m.FundingValue == 500));
        }


        private static Dictionary<string, IEnumerable<FundingLine>> SetUpInput()
        {
            List<FundingLine> fundingLine = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Payment,Value = 500, TemplateLineId = 123, DistributionPeriods = null}

            };

            Dictionary<string, IEnumerable<FundingLine>> fundingLines = new Dictionary<string, IEnumerable<FundingLine>>
            {
                { "test", fundingLine }

            };
            return fundingLines;
        }

        private static Dictionary<string, IEnumerable<FundingLine>> ExpectedOutput()
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

            List<FundingLine> fundingLine = new List<FundingLine>
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Payment,Value = 500, TemplateLineId = 123,
                    DistributionPeriods = distributionPeriod}

            };

            Dictionary<string, IEnumerable<FundingLine>> fundingLines = new Dictionary<string, IEnumerable<FundingLine>>
            {
                { "test", fundingLine }

            };

            return fundingLines;
        }
        private static ValidatedApiResponse<ProviderProfilingResponseModel> SetUpProviderProfilingResponse()
        {
            return new ValidatedApiResponse<ProviderProfilingResponseModel>(HttpStatusCode.OK, new ProviderProfilingResponseModel()
            {
                DeliveryProfilePeriods = new List<Common.ApiClient.Profiling.Models.ProfilingPeriod>
                 {
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "October", Occurrence = 1, Year = 2018, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" },
                    new Common.ApiClient.Profiling.Models.ProfilingPeriod { Period = "April", Occurrence = 1, Year = 2019, Type = "CalendarMonth", Value = 82190.0M, DistributionPeriod = "2018-2019" }
                 },
                DistributionPeriods = new List<Common.ApiClient.Profiling.Models.DistributionPeriods>
                 {
                    new Common.ApiClient.Profiling.Models.DistributionPeriods { DistributionPeriodCode = "2018-2019",   Value = 82190.0M }                    
                 }
            });
        }

        protected void GivenTheApiResponseDetailsForTheProfileRequest(ProviderProfilingResponseModel providerProfilingResponseModel,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            profilingClient1  = CreateProfilingRepository();
            profilingClient1.GetProviderProfilePeriods(Arg.Any<ProviderProfilingRequestModel>())
                .Returns(new ValidatedApiResponse<ProviderProfilingResponseModel>(statusCode,
                    providerProfilingResponseModel));
        }

        static ProfilingService CreateProfilingService(ILogger logger = null,            
            IProfilingApiClient profilingApiClient = null)
        {
            return new ProfilingService(
                logger ?? CreateLogger(),               
                profilingApiClient ?? CreateProfilingRepository()
               
                );
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
