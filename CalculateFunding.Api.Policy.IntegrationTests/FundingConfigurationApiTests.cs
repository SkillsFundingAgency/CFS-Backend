using CalculateFunding.Api.Policy.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Policies.Models.ViewModels;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Policy.IntegrationTests
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class FundingConfigurationApiTests : IntegrationTest
    {
        private FundingConfigurationDataContext _fundingConfigurationDataContext;
        private FundingStreamDataContext _fundingStreamDataContext;
        private FundingPeriodDataContext _fundingPeriodDataContext;
        private FundingTemplateDataContext _fundingTemplateDataContext;
        private IPoliciesApiClient _policiesClient;

        [ClassInitialize]
        public static void FixtureSetup(TestContext testConext)
        {
            SetUpConfiguration();
            SetUpServices((sc, c) =>
                    sc.AddPoliciesInterServiceClient(c),
                    AddCacheProvider,
                    AddNullLogger,
                    AddUserProvider);
        }

        [TestInitialize]
        public void Setup()
        {
            _fundingConfigurationDataContext = new FundingConfigurationDataContext(Configuration);
            _fundingStreamDataContext = new FundingStreamDataContext(Configuration);
            _fundingPeriodDataContext = new FundingPeriodDataContext(Configuration);
            _fundingTemplateDataContext = new FundingTemplateDataContext(Configuration);

            TrackForTeardown(_fundingConfigurationDataContext);
            TrackForTeardown(_fundingStreamDataContext);
            TrackForTeardown(_fundingPeriodDataContext);
            TrackForTeardown(_fundingTemplateDataContext);

            _policiesClient = GetService<IPoliciesApiClient>();
        }

        [TestMethod]
        public async Task ShouldSaveAllowedPublishedFundingStreamsIdsToReferenceForExistingFundingConfiguration()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string defaultTemplateVersion = "1.0";
            string allowedPublishedFundingStreamId = NewRandomString();
            string fundingVersion = "1_0";

            FundingStreamParameters fundingStreamParameters = NewFundingStreamPatameters(_ =>
                                                                _.WithId(fundingStreamId)
                                                                 .WithName(fundingStreamId)
                                                                 .WithShortName(fundingStreamId));

            FundingPeriodParameters fundingPeriodParameters = NewFundingPeriodPatameters(_ =>
                                                            _.WithId(fundingPeriodId)
                                                             .WithName(fundingPeriodId));

            FundingTemplateParameters fundingTemplateParameters = NewFundingTemplatePatameters(_ =>
                                                            _.WithId($"{fundingStreamId}-{fundingPeriodId}-RA-{fundingVersion}")
                                                             .WithFundingStreamId(fundingStreamId)
                                                             .WithFundingPeriodId(fundingPeriodId)
                                                             .WithFundingStreamName(fundingStreamId)
                                                             .WithFundingVersion(fundingVersion)
                                                             .WithTemplateVersion(defaultTemplateVersion));

            FundingConfigurationParameters fundingConfigurationPatameters = NewFundingConfigurationPatameters(_ =>
                         _.WithFundingStreamId(fundingStreamId)
                         .WithFundingPeriodId(fundingPeriodId)
                         .WithDefaultTemplateVersion(defaultTemplateVersion));

            FundingStreamParameters allowedFundingStreamParameters = NewFundingStreamPatameters(_ =>
                                                                _.WithId(allowedPublishedFundingStreamId)
                                                                 .WithName(allowedPublishedFundingStreamId)
                                                                 .WithShortName(allowedPublishedFundingStreamId));

            await GivenFundingStream(fundingStreamParameters);
            await AndFundingStream(allowedFundingStreamParameters);
            await AndFundingPeriod(fundingPeriodParameters);
            await AndFundingTemplate(fundingTemplateParameters);
            await AndFundingConfiguration(fundingConfigurationPatameters);

            FundingConfigurationUpdateViewModel fundingConfigurationUpdateViewModel = NewFundingConfigurationUpdateViewModel(_ =>
                                _.WithDefaultTemplateVersion(defaultTemplateVersion)
                                .WithApprovalMode(ApprovalMode.All)
                                .WithUpdateCoreProviderVersion(UpdateCoreProviderVersion.Manual)
                                .WithAllowedPublishedFundingStreamsIdsToReference(allowedPublishedFundingStreamId));

            ApiResponse<FundingConfiguration> response = await WhenTheFundingConfigurationUpdated(fundingStreamId, fundingPeriodId, fundingConfigurationUpdateViewModel);

            // NOTE: Save fundingconfiguration return Created status code without any content. Need to change in common client package.
            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"Save funding configuration request failed with status code {response.StatusCode}");

            response = await _policiesClient.GetFundingConfiguration(fundingStreamId, fundingPeriodId);

            response.StatusCode
               .IsSuccess()
               .Should()
               .BeTrue($"Get funding configuration request failed with status code {response.StatusCode}");

            FundingConfiguration fundingConfiguration = response?.Content;

            fundingConfiguration
                .Should()
                .NotBeNull();

            fundingConfiguration
                .AllowedPublishedFundingStreamsIdsToReference
                .Should()
                .BeEquivalentTo(new[] { allowedPublishedFundingStreamId });
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private Task<ApiResponse<FundingConfiguration>> WhenTheFundingConfigurationUpdated(
            string fundingStreamId,
            string fundingPeriodId,
            FundingConfigurationUpdateViewModel fundingConfigurationUpdateViewModel)
        {
            return _policiesClient.SaveFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfigurationUpdateViewModel);
        }

        private async Task AndFundingConfiguration(FundingConfigurationParameters fundingConfigurationPatameters)
        {
            await _fundingConfigurationDataContext.CreateContextData(fundingConfigurationPatameters);
        }

        private async Task AndFundingStream(FundingStreamParameters fundingStreamPatameters)
        {
            await GivenFundingStream(fundingStreamPatameters);
        }
        private async Task GivenFundingStream(FundingStreamParameters fundingStreamPatameters)
        {
            await _fundingStreamDataContext.CreateContextData(fundingStreamPatameters);
        }

        private async Task AndFundingPeriod(FundingPeriodParameters fundingPeriodPatameters)
        {
            await _fundingPeriodDataContext.CreateContextData(fundingPeriodPatameters);
        }

        private async Task AndFundingTemplate(FundingTemplateParameters fundingTemplatePatameters)
        {
            await _fundingTemplateDataContext.CreateContextData(fundingTemplatePatameters);
        }

        private FundingConfigurationUpdateViewModel NewFundingConfigurationUpdateViewModel(Action<FundingConfigurationUpdateViewModelBuilder> setUp = null)
        {
            FundingConfigurationUpdateViewModelBuilder builder = new FundingConfigurationUpdateViewModelBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private FundingConfigurationParameters NewFundingConfigurationPatameters(Action<FundingConfigurationParametersBuilder> setUp = null)
        {
            FundingConfigurationParametersBuilder builder = new FundingConfigurationParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private FundingStreamParameters NewFundingStreamPatameters(Action<FundingStreamParametersBuilder> setUp = null)
        {
            FundingStreamParametersBuilder builder = new FundingStreamParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private FundingPeriodParameters NewFundingPeriodPatameters(Action<FundingPeriodParametersBuilder> setUp = null)
        {
            FundingPeriodParametersBuilder builder = new FundingPeriodParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private FundingTemplateParameters NewFundingTemplatePatameters(Action<FundingTemplateParametersBuilder> setUp = null)
        {
            FundingTemplateParametersBuilder builder = new FundingTemplateParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }
    }
}
