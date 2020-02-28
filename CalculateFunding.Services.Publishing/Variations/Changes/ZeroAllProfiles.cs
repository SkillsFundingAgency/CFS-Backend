using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ZeroAllProfiles : VariationChange
    {
        public ZeroAllProfiles(ProviderVariationContext variationContext)
            : base(variationContext)
        {
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            RefreshState.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment).ToList().ForEach(fl =>
            {
                fl.Value = 0;

                fl.DistributionPeriods?
                    .SelectMany(_ => _.ProfilePeriods ?? new ProfilePeriod[0])
                    .ToList()
                    .ForEach(_ => _.ProfiledValue = 0);
            });

            return Task.CompletedTask;
        }
    }
}