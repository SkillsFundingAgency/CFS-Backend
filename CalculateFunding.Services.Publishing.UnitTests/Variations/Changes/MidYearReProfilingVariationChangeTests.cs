using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MidYearReProfilingVariationChangeTests : ReProfilingVariationChangeTestsBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new MidYearReProfileVariationChange(VariationContext);
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
    }
}