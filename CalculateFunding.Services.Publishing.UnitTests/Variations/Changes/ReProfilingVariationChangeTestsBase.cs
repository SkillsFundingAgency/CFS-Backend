using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class ReProfilingVariationChangeTestsBase : VariationChangeTestBase
    {
        protected Mock<IReProfilingRequestBuilder> ReProfileRequestBuilder;
        
        private Mock<IReProfilingResponseMapper> _reProfilingResponseMapper;
        private Mock<IProfilingApiClient> _profilingApiClient;

        [TestInitialize]
        public void ReProfilingVariationChangeTestsBaseSetUp()
        {
            _reProfilingResponseMapper = new Mock<IReProfilingResponseMapper>();
            ReProfileRequestBuilder = new Mock<IReProfilingRequestBuilder>();
            _profilingApiClient = new Mock<IProfilingApiClient>();

            VariationsApplication.ReProfilingResponseMapper
                .Returns(_reProfilingResponseMapper.Object);
            VariationsApplication.ReProfilingRequestBuilder
                .Returns(ReProfileRequestBuilder.Object);
            VariationsApplication.ProfilingApiClient
                .Returns(_profilingApiClient.Object);
        }

        [TestMethod]
        public void GuardsAgainstNoAffectedFundingLineCodesInContext()
        {
            Action invocation = () => WhenTheChangeIsApplied()
                .GetAwaiter()
                .GetResult();

            invocation.Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("AffectedFundingLineCodes");
        }

        [TestMethod]
        public async Task ReProfilesFundingLinesInTheRefreshStateWhereTheyShowAsAffectedFundingLineCodes()
        {
            FundingLine fundingLineOne = NewFundingLine();
            FundingLine fundingLineTwo = NewFundingLine();
            FundingLine fundingLineThree = NewFundingLine();
            ProfilePatternKey profilePatternKey = NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode));

            ReProfileRequest reProfileRequestOne = NewReProfileRequest();
            ReProfileRequest reProfileRequestThree = NewReProfileRequest();

            ReProfileResponse reProfileResponseOne = NewReProfileResponse();
            ReProfileResponse reProfileResponseThree = NewReProfileResponse();

            DistributionPeriod[] distributionPeriodsOne = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsTwo = NewDistributionPeriods();
            DistributionPeriod[] distributionPeriodsThree = NewDistributionPeriods();

            fundingLineOne.DistributionPeriods = NewDistributionPeriods(_ => _.WithDistributionPeriodId(distributionPeriodsOne.Single().DistributionPeriodId));
            fundingLineTwo.DistributionPeriods = distributionPeriodsTwo;
            fundingLineThree.DistributionPeriods = NewDistributionPeriods(_ => _.WithDistributionPeriodId(distributionPeriodsThree.Single().DistributionPeriodId));
            
            GivenTheFundingLines(fundingLineOne, fundingLineTwo, fundingLineThree);
            GivenTheProfilePatternKeys(NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineOne.FundingLineCode)
                .WithKey(profilePatternKey.Key)));
            AndTheAffectedFundingLineCodes(fundingLineOne.FundingLineCode, fundingLineThree.FundingLineCode);
            AndTheTheReProfileRequest(fundingLineOne, reProfileRequestOne, RefreshState.ProfilePatternKeys.Single(_ => _.FundingLineCode == fundingLineOne.FundingLineCode).Key);
            AndTheTheReProfileRequest(fundingLineThree, reProfileRequestThree);
            AndTheReProfileResponse(reProfileRequestOne, reProfileResponseOne);
            AndTheReProfileResponse(reProfileRequestThree, reProfileResponseThree);
            AndTheReProfileResponseMapping(reProfileResponseOne, distributionPeriodsOne);
            AndTheReProfileResponseMapping(reProfileResponseThree, distributionPeriodsThree);

            await WhenTheChangeIsApplied();

            fundingLineOne.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsOne);

            fundingLineTwo.DistributionPeriods
                .Should()
                .BeSameAs(distributionPeriodsTwo);
            
            fundingLineThree.DistributionPeriods
                .Should()
                .BeEquivalentTo<DistributionPeriod>(distributionPeriodsThree);
        }

        private DistributionPeriod[] NewDistributionPeriods(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            ProfilePeriod profilePeriodOne = NewProfilePeriod();
            ProfilePeriod profilePeriodTwo = NewProfilePeriod();
            distributionPeriodBuilder.WithProfilePeriods(profilePeriodOne,
                profilePeriodTwo);
            distributionPeriodBuilder.WithValue(profilePeriodOne.ProfiledValue + profilePeriodTwo.ProfiledValue);
            
            return new [] { distributionPeriodBuilder.Build() };
        }

        private ReProfileRequest NewReProfileRequest() => new ReProfileRequest();
        private ReProfileResponse NewReProfileResponse() => new ReProfileResponse();

        private void AndTheReProfileResponse(ReProfileRequest request,
            ReProfileResponse response)
            => _profilingApiClient.Setup(_ => _.ReProfile(request))
                .ReturnsAsync(new ApiResponse<ReProfileResponse>(HttpStatusCode.OK, response));

        protected virtual void AndTheTheReProfileRequest(FundingLine fundingLine,
            ReProfileRequest reProfileRequest,
            string key = null)
            => ReProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(RefreshState.FundingStreamId,
                    RefreshState.SpecificationId,
                    RefreshState.FundingPeriodId,
                    RefreshState.ProviderId,
                    fundingLine.FundingLineCode,
                    key,
                    ProfileConfigurationType.RuleBased,
                    fundingLine.Value,
                    false))
                .ReturnsAsync(reProfileRequest);

        private void AndTheReProfileResponseMapping(ReProfileResponse reProfileResponse,
            IEnumerable<DistributionPeriod> distributionPeriods)
            => _reProfilingResponseMapper.Setup(_ => _.MapReProfileResponseIntoDistributionPeriods(reProfileResponse))
                .Returns(distributionPeriods);

        private void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => VariationContext.AffectedFundingLineCodes = fundingLineCodes.ToList();
    }
}