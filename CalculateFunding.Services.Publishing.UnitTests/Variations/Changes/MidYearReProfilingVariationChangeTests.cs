using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MidYearReProfilingVariationChangeTests : ReProfilingVariationChangeTestsBase
    {
        protected override string Strategy => "MidYearReProfiling";

        [TestInitialize]
        public void SetUp()
        {
            Change = new MidYearReProfileVariationChange(VariationContext, Strategy);
        }

        protected override void AndTheTheReProfileRequest(FundingLine fundingLine,
            ReProfileRequest reProfileRequest,
            string key = null)
            => ReProfileRequestBuilder.Setup(_ => _.BuildReProfileRequest(fundingLine.FundingLineCode,
                    key,
                    RefreshState,
                    ProfileConfigurationType.RuleBased,
                    fundingLine.Value,
                    null,
                    true))
                .ReturnsAsync(reProfileRequest);

        protected override void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => fundingLineCodes.ForEach(_ => VariationContext.AddAffectedFundingLineCode(Strategy, _));

    }
}