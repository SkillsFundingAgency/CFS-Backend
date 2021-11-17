using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MidYearClosureReProfilingVariationChangeTests : ReProfilingVariationChangeTestsBase
    {
        protected override string Strategy => "MidYearClosureReProfiling";

        [TestInitialize]
        public void SetUp()
        {
            Change = new MidYearClosureReProfileVariationChange(VariationContext, Strategy);
        }

        protected override void AndTheTheReProfileRequest(FundingLine fundingLine,
            ReProfileRequest reProfileRequest,
            string key = null)
            => ReProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(fundingLine.FundingLineCode,
                    key,
                    PriorState,
                    ProfileConfigurationType.RuleBased,
                    fundingLine.Value,
                    MidYearType.Closure))
                .ReturnsAsync(reProfileRequest);

        protected override void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => fundingLineCodes.ForEach(_ => VariationContext.AddAffectedFundingLineCode(Strategy, _));

    }
}