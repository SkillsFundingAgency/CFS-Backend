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
    public class MidYearReProfilingVariationChangeTests : ReProfilingVariationChangeTestsBase
    {
        protected override string Strategy => "MidYearReProfiling";

        protected DateTimeOffset OpenDate => ProfileDate.AddMonths(1);

        protected virtual MidYearType MidYearTypeValue => MidYearType.Opener;

        protected virtual PublishedProviderVersion ReProfilePublishedProvider => RefreshState;

        [TestInitialize]
        public virtual void SetUp()
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
                    ReProfilePublishedProvider,
                    fundingLine.Value,
                    reProfileAudit,
                    MidYearTypeValue,
                    It.IsAny<Func<string, ReProfileAudit, int, bool>>()))
                .ReturnsAsync((reProfileRequest, ((MidYearReProfileVariationChange)Change).ReProfileForSameAmountFunc(fundingLine.FundingLineCode, reProfileAudit, reProfileRequest.VariationPointerIndex ?? 2)));
        }

        protected override void AndTheAffectedFundingLineCodes(params string[] fundingLineCodes)
            => fundingLineCodes.ForEach(_ => VariationContext.AddAffectedFundingLineCode(Strategy, _));

    }
}