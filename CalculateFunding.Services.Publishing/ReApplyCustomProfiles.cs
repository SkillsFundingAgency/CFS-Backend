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
        public bool ProcessPublishedProvider(PublishedProviderVersion publishedProviderVersion)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));

            bool updatePublishedProvider = false;

            if (!publishedProviderVersion.HasCustomProfiles)
            {
                return updatePublishedProvider;
            }

            Dictionary<string, FundingLine> fundingLines = publishedProviderVersion.FundingLines?.Where(_ => _.Type == FundingLineType.Payment)?.ToDictionary(_ => _.FundingLineCode) 
                                                           ?? new Dictionary<string, FundingLine>();

            foreach (FundingLineProfileOverrides customProfile in publishedProviderVersion.CustomProfiles)
            {
                string fundingLineCode = customProfile.FundingLineCode;

                if (!fundingLines.TryGetValue(fundingLineCode, out FundingLine fundingLine))
                {
                    throw new InvalidOperationException(
                        $"Custom profile has no matching funding line for {fundingLineCode} on {publishedProviderVersion.Id}");
                }

                updatePublishedProvider = true;

                fundingLine.DistributionPeriods = customProfile.DistributionPeriods.DeepCopy();
            }

            return updatePublishedProvider;
        }
    }
}