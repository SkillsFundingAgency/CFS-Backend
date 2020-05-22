using AutoMapper;
using CalculateFunding.Api.External.V3.MappingProfiles;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Api.External.V3.Services;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PolicyApiClientModel = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Api.External.UnitTests.Version3.Services
{
    [TestClass]
    public class FundingStreamServiceTests
    {
        private IPoliciesApiClient _policiesApiClient;
        private IMapper _mapper;
        private FundingStreamService _fundingStreamService;

        private string _fundingStreamId;
        private string _fundingStreamName;
        private string _fundingPeriodId;
        private string _fundingPeriodName;
        private string _fundingPeriodDefaultTemplateVersion;

        [TestInitialize]
        public void Initialize()
        {
            _fundingStreamId = NewRandomString();
            _fundingStreamName = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _fundingPeriodName = NewRandomString();
            _fundingPeriodDefaultTemplateVersion = NewRandomString();

            _policiesApiClient = Substitute.For<IPoliciesApiClient>();
            _mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<ExternalServiceMappingProfile>();
            }).CreateMapper();

            _fundingStreamService = new FundingStreamService(
                _policiesApiClient,
                new ExternalApiResiliencePolicies
                {
                    PoliciesApiClientPolicy = Polly.Policy.NoOpAsync()
                },
                _mapper
                );
        }

        [TestMethod]
        public async Task ReturnsFundingStreams()
        {
            IEnumerable<PolicyApiClientModel.FundingStream> apiFundingStreams = 
                new List<PolicyApiClientModel.FundingStream>
            {
                NewApiFundingStream(_ => _.WithId(_fundingStreamId).WithName(_fundingStreamName))
            };

            GivenApiFundingStreams(apiFundingStreams);

            IActionResult result = await WhenGetFundingStreams();

            ThenFundingStreamResultMatches(result);
        }

        [TestMethod]
        public void ThrowsArgumentExceptionOnGivenFundingStreamId()
        {
            Func<Task> test = async () => await WhenGetFundingPeriods(null);

            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundWhenNoFundingConfigurationWithGivenFundingStream()
        {
            GivenApiFundingConfiguration(null);

            IActionResult result = await WhenGetFundingPeriods(_fundingStreamId);

            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Funding configuration not found for funding stream: {_fundingStreamId}");
        }

        [TestMethod]
        public async Task ReturnsFundingPeriodsGivenFundingStream()
        {
            IEnumerable<PolicyApiClientModel.FundingConfig.FundingConfiguration> apiFundingConfigs =
                new List<PolicyApiClientModel.FundingConfig.FundingConfiguration>
            {
                NewApiFundingConfig(_ => _
                    .WithFundingStreamId(_fundingStreamId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithDefaultTemplateVersion(_fundingPeriodDefaultTemplateVersion))
            };

            IEnumerable<PolicyApiClientModel.FundingPeriod> apiFundingPeriods =
                new List<PolicyApiClientModel.FundingPeriod>
            {
                NewApiFundingPeriod(_ => _
                    .WithId(_fundingPeriodId)
                    .WithName(_fundingPeriodName))
            };

            GivenApiFundingConfiguration(apiFundingConfigs);
            GivenApiFundingPeriods(apiFundingPeriods);

            IActionResult result = await WhenGetFundingPeriods(_fundingStreamId);

            ThenFundingPeriodResultMatches(result);
        }

        private void GivenApiFundingStreams(IEnumerable<PolicyApiClientModel.FundingStream> fundingStreams)
        {
            _policiesApiClient
                .GetFundingStreams()
                .Returns(
                Task.FromResult(
                    new ApiResponse<IEnumerable<PolicyApiClientModel.FundingStream>>(
                        HttpStatusCode.OK,
                        fundingStreams)));
        }

        private void GivenApiFundingConfiguration(IEnumerable<PolicyApiClientModel.FundingConfig.FundingConfiguration> fundingConfigurations)
        {
            _policiesApiClient
                .GetFundingConfigurationsByFundingStreamId(_fundingStreamId)
                .Returns(
                    Task.FromResult(
                        new ApiResponse<IEnumerable<PolicyApiClientModel.FundingConfig.FundingConfiguration>>(
                            HttpStatusCode.OK,
                            fundingConfigurations)));
        }

        private void GivenApiFundingPeriods(IEnumerable<PolicyApiClientModel.FundingPeriod> fundingPeriods)
        {
            _policiesApiClient
                .GetFundingPeriods()
                .Returns(
                Task.FromResult(
                    new ApiResponse<IEnumerable<PolicyApiClientModel.FundingPeriod>>(
                        HttpStatusCode.OK,
                        fundingPeriods)));
        }

        private async Task<IActionResult> WhenGetFundingStreams()
        {
            return await _fundingStreamService.GetFundingStreams();
        }

        private async Task<IActionResult> WhenGetFundingPeriods(string fundingStreamId)
        {
            return await _fundingStreamService.GetFundingPeriods(fundingStreamId);
        }

        private void ThenFundingPeriodResultMatches(IActionResult result)
        {
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<List<FundingPeriod>>();

            IEnumerable<FundingPeriod> fundingPeriods = (result as OkObjectResult).Value as IEnumerable<FundingPeriod>;

            fundingPeriods
                .Count()
                .Should()
                .Be(1);

            fundingPeriods
                .FirstOrDefault()
                .Should()
                .NotBeNull();

            FundingPeriod fundingPeriod = fundingPeriods.FirstOrDefault();

            fundingPeriod
                .Id
                .Should()
                .Be(_fundingPeriodId);

            fundingPeriod
                .Name
                .Should()
                .Be(_fundingPeriodName);

            fundingPeriod
                .DefaultTemplateVersion
                .Should()
                .Be(_fundingPeriodDefaultTemplateVersion);
        }

        private void ThenFundingStreamResultMatches(IActionResult result)
        {
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<List<FundingStream>>();

            IEnumerable<FundingStream> fundingStreams = (result as OkObjectResult).Value as IEnumerable<FundingStream>;

            fundingStreams
                .Count()
                .Should()
                .Be(1);

            fundingStreams
                .FirstOrDefault()
                .Should()
                .NotBeNull();

            FundingStream fundingStream = fundingStreams.FirstOrDefault();

            fundingStream
                .Id
                .Should()
                .Be(_fundingStreamId);

            fundingStream
                .Name
                .Should()
                .Be(_fundingStreamName);
        }

        private PolicyApiClientModel.FundingStream NewApiFundingStream(
            Action<PolicyFundingStreamBuilder> setUp = null)
        {
            PolicyFundingStreamBuilder fundingStreamBuilder = new PolicyFundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        private PolicyApiClientModel.FundingConfig.FundingConfiguration NewApiFundingConfig(
            Action<PolicyFundingConfigurationBuilder> setUp = null)
        {
            PolicyFundingConfigurationBuilder fundingConfigBuilder = new PolicyFundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigBuilder);

            return fundingConfigBuilder.Build();
        }

        private PolicyApiClientModel.FundingPeriod NewApiFundingPeriod(
            Action<PolicyFundingPeriodBuilder> setUp = null)
        {
            PolicyFundingPeriodBuilder fundingPeriodBuilder = new PolicyFundingPeriodBuilder();

            setUp?.Invoke(fundingPeriodBuilder);

            return fundingPeriodBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
