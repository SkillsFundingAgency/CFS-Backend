using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MidYearReProfilingWithCatchUpVariationChangeTests : MidYearReProfilingVariationChangeTests
    {
        protected override string Strategy => "MidYearReProfiling";

        protected DateTimeOffset OpenDate => ProfileDate.AddMonths(-1);

        [TestInitialize]
        public void SetUp()
        {
            Change = new MidYearReProfileVariationChange(VariationContext, Strategy);
            VariationContext.RefreshState.Provider = NewProvider(_ => _.WithDateOpened(OpenDate));
        }

        protected override void AndTheTheReProfileRequest(FundingLine fundingLine,
            ReProfileRequest reProfileRequest,
            string key = null,
            string eTag = "ETag",
            int variationPointerIndex = 1)
        {
            ProfilePeriod firstPeriod = new YearMonthOrderedProfilePeriods(fundingLine).ToArray().First();
            bool catchup = OpenDate == null ? false : OpenDate.Month < YearMonthOrderedProfilePeriods.MonthNumberFor(firstPeriod.TypeValue) && OpenDate.Year <= firstPeriod.Year;

            VariationContext.ProfilePatterns = (VariationContext.ProfilePatterns?.Values ?? ArraySegment<FundingStreamPeriodProfilePattern>.Empty).Concat((new[] { new FundingStreamPeriodProfilePattern {
                FundingLineId = fundingLine.FundingLineCode,
                ETag = eTag
            } })).ToDictionary(_ => _.FundingLineId);

            PublishedProviderVersion publishedProvider = VariationContext.RefreshState;

            ReProfileAudit reProfileAudit = new ReProfileAudit
            {
                FundingLineCode = fundingLine.FundingLineCode,
                VariationPointerIndex = variationPointerIndex
            };

            publishedProvider.AddOrUpdateReProfileAudit(reProfileAudit);

            ReProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(fundingLine.FundingLineCode,
                    key,
                    RefreshState,
                    fundingLine.Value,
                    reProfileAudit,
                    catchup ? MidYearType.OpenerCatchup : MidYearType.Opener,
                    It.IsAny<Func<string, ReProfileAudit, int, bool>>()))
                .ReturnsAsync((reProfileRequest, ((MidYearReProfileVariationChange)Change).ReProfileForSameAmountFunc(fundingLine.FundingLineCode, reProfileAudit, reProfileRequest.VariationPointerIndex ?? 2)));
        }
    }
}