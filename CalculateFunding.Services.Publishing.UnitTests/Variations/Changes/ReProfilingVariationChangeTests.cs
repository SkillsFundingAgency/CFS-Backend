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
using CalculateFunding.Services.Publishing.Variations.Changes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReProfilingVariationChangeTests : VariationChangeTestBase
    {
        private Mock<IReProfilingResponseMapper> _reProfilingResponseMapper;
        private Mock<IReProfilingRequestBuilder> _reProfileRequestBuilder;
        private Mock<IProfilingApiClient> _profilingApiClient;
        
        [TestInitialize]
        public void SetUp()
        {
            _reProfilingResponseMapper = new Mock<IReProfilingResponseMapper>();
            _reProfileRequestBuilder = new Mock<IReProfilingRequestBuilder>();
            _profilingApiClient = new Mock<IProfilingApiClient>();

            VariationsApplication.ReProfilingResponseMapper
                .Returns(_reProfilingResponseMapper.Object);
            VariationsApplication.ReProfilingRequestBuilder
                .Returns(_reProfileRequestBuilder.Object);
            VariationsApplication.ProfilingApiClient
                .Returns(_profilingApiClient.Object);
            
            Change = new ReProfileVariationChange(VariationContext);
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
            AndTheAffectedFundingLineCodes(fundingLineOne.FundingLineCode, fundingLineThree.FundingLineCode);
            AndTheTheReProfileRequest(fundingLineOne, reProfileRequestOne);
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

        private void AndTheTheReProfileRequest(FundingLine fundingLine,
            ReProfileRequest reProfileRequest)
            => _reProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(RefreshState.SpecificationId,
                RefreshState.FundingStreamId,
                RefreshState.FundingPeriodId,
                RefreshState.ProviderId,
                fundingLine.FundingLineCode,
                null,
                ProfileConfigurationType.RuleBased,
                fundingLine.Value))
                .ReturnsAsync(reProfileRequest);

        private void AndTheReProfileResponseMapping(ReProfileResponse reProfileResponse,
            IEnumerable<DistributionPeriod> distributionPeriods)
            => _reProfilingResponseMapper.Setup(_ => _.MapReProfileResponseIntoDistributionPeriods(reProfileResponse))
                .Returns(distributionPeriods);

        private void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => VariationContext.AffectedFundingLineCodes = fundingLineCodes.ToList();
    }
}