using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing
{
    public class ReApplyCustomProfiles : IReApplyCustomProfiles
    {
        public void ProcessPublishedProvider(PublishedProviderVersion publishedProviderVersion, GeneratedProviderResult generatedProviderResult)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(generatedProviderResult, nameof(generatedProviderResult));

            if (!publishedProviderVersion.HasCustomProfiles)
            {
                return;
            }

            Dictionary<string, FundingLine> fundingLines = generatedProviderResult.FundingLines?.Where(_ => _.Type == FundingLineType.Payment)?.ToDictionary(_ => _.FundingLineCode) 
                                                           ?? new Dictionary<string, FundingLine>();

            foreach (FundingLineProfileOverrides customProfile in publishedProviderVersion.CustomProfiles)
            {
                string fundingLineCode = customProfile.FundingLineCode;

                if (!fundingLines.TryGetValue(fundingLineCode, out FundingLine fundingLine))
                {
                    throw new InvalidOperationException(
                        $"Custom profile has no matching funding line for {fundingLineCode} on {publishedProviderVersion.Id}");
                }

                fundingLine.DistributionPeriods = customProfile.DistributionPeriods.DeepCopy();
            }

            return;
        }
    }
}