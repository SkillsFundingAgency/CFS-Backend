using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class ReApplyCustomProfiles : IReApplyCustomProfiles
    {
        private readonly IPublishedProviderErrorDetection _detection;

        public ReApplyCustomProfiles(IPublishedProviderErrorDetection detection)
        {
            Guard.ArgumentNotNull(detection, nameof(detection));
            
            _detection = detection;
        }

        public async Task ProcessPublishedProvider(PublishedProviderVersion publishedProviderVersion)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));

            if (!publishedProviderVersion.HasCustomProfiles)
            {
                return;
            }

            Dictionary<string, FundingLine> fundingLines = publishedProviderVersion.FundingLines?.ToDictionary(_ => _.FundingLineCode) 
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

            await _detection.ProcessPublishedProvider(publishedProviderVersion);
        }
    }
}