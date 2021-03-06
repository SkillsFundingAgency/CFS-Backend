﻿using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

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
            RefreshState.FundingLines.Where(_ => _.Type == FundingLineType.Payment).ToList().ForEach(fl =>
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