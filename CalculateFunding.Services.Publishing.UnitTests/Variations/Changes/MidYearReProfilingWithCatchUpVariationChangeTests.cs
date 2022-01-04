using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MidYearReProfilingWithCatchUpVariationChangeTests : ReProfilingVariationChangeTestsBase
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
            string key = null)
        {
            ProfilePeriod firstPeriod = new YearMonthOrderedProfilePeriods(fundingLine).ToArray().First();
            bool catchup = OpenDate == null ? false : OpenDate.Month < YearMonthOrderedProfilePeriods.MonthNumberFor(firstPeriod.TypeValue) && OpenDate.Year <= firstPeriod.Year;
            ReProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(fundingLine.FundingLineCode,
                    key,
                    RefreshState,
                    ProfileConfigurationType.RuleBased,
                    fundingLine.Value,
                    catchup ? MidYearType.OpenerCatchup : MidYearType.Opener))
                .ReturnsAsync(reProfileRequest);
        }

        protected override void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => fundingLineCodes.ForEach(_ => VariationContext.AddAffectedFundingLineCode(Strategy, _));

    }
}