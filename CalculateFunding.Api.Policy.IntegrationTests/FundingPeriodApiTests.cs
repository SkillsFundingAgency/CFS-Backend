using CalculateFunding.Api.Policy.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using ApiModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Config.ApiClient.Policies;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Api.Policy.IntegrationTests
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class FundingPeriodApiTests : IntegrationTest
    {
        private FundingPeriodDataContext _fundingPeriodDataContext;
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
            _fundingPeriodDataContext = new FundingPeriodDataContext(Configuration);

            TrackForTeardown(_fundingPeriodDataContext);

            _policiesClient = GetService<IPoliciesApiClient>();
        }

        [TestMethod]
        public async Task ShouldGetExistingFundingPeriod()
        {
            string fundingPeriodId = NewRandomString();
            
            FundingPeriodParameters fundingPeriodParameters = NewFundingPeriodPatameters(_ =>
                                                            _.WithId(fundingPeriodId)
                                                             .WithName(fundingPeriodId));

            await GivenFundingPeriod(fundingPeriodParameters);

            ApiResponse<ApiModels.FundingPeriod> response = await _policiesClient.GetFundingPeriodById(fundingPeriodId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"Get funding period by id request failed with status code {response.StatusCode}");

            ApiModels.FundingPeriod existingPeriodResponse = response?.Content;

            existingPeriodResponse
                .Should()
                .NotBeNull();

            existingPeriodResponse
                .Id
                .Should()
                .Be(fundingPeriodId, $"Get funding period returned {existingPeriodResponse.Id} asked for {fundingPeriodId}");
        }

        [TestMethod]
        public async Task ShouldSaveValidFundingPeriod()
        {
            string fundingPeriodId = NewRandomString();
                        
            FundingPeriodParameters fundingPeriodParameters = NewFundingPeriodPatameters(_ =>
                                                            _.WithId(fundingPeriodId)
                                                             .WithName(fundingPeriodId));

            FundingPeriod validFundingPeriod = GivenNewValidFundingPeriod(fundingPeriodParameters);

            ApiModels.FundingPeriodsUpdateModel fundingPeriodsUpdateModel = NewFundingPeriodUpdateModel(_ =>
                                _.WithId(validFundingPeriod.Id)
                                .WithName(validFundingPeriod.Name)
                                .WithStartDate(validFundingPeriod.StartDate)
                                .WithEndDate(validFundingPeriod.EndDate)
                                .WithFundingType(Enum.TryParse(validFundingPeriod.Type.ToString(), out ApiModels.FundingPeriodType periodType) ? periodType : ApiModels.FundingPeriodType.AC)
                                .WithPeriod(validFundingPeriod.Period));

            ApiResponse<ApiModels.FundingPeriod> response = await WhenTheFundingPeriodSaved(fundingPeriodsUpdateModel);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"Get funding periods request failed with status code {response.StatusCode}");

            response = await _policiesClient.GetFundingPeriodById(fundingPeriodId);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"Get funding period by id request failed with status code {response.StatusCode}");

            ApiModels.FundingPeriod newlySavedPeriodResponse = response?.Content;

            newlySavedPeriodResponse
                .Should()
                .NotBeNull();

            newlySavedPeriodResponse
                .Id
                .Should()
                .Be(fundingPeriodId, $"Get funding period returned {newlySavedPeriodResponse.Id} asked for {fundingPeriodId}");
        }

        private Task<ApiResponse<ApiModels.FundingPeriod>> WhenTheFundingPeriodSaved(ApiModels.FundingPeriodsUpdateModel fundingPeriodsUpdateModel)
        {
            return _policiesClient.SaveFundingPeriods(fundingPeriodsUpdateModel);
        }

        private async Task GivenFundingPeriod(FundingPeriodParameters fundingPeriodPatameters)
        {
            await _fundingPeriodDataContext.CreateContextData(fundingPeriodPatameters);
        }

        private FundingPeriod GivenNewValidFundingPeriod(FundingPeriodParameters fundingPeriodParameters)
        {
            return _fundingPeriodDataContext.GetFundingPeriod(fundingPeriodParameters);
        }

        private ApiModels.FundingPeriodsUpdateModel NewFundingPeriodUpdateModel(Action<FundingPeriodsUpdateModelBuilder> setUp = null)
        {
            FundingPeriodsUpdateModelBuilder builder = new FundingPeriodsUpdateModelBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private FundingPeriodParameters NewFundingPeriodPatameters(Action<FundingPeriodParametersBuilder> setUp = null)
        {
            FundingPeriodParametersBuilder builder = new FundingPeriodParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }
    }
}
