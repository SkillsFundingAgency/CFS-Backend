using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Linq;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class InformationLineAggregator : IInformationLineAggregator
    {
        private IDictionary<string, ProfilePeriod> _aggregatePeriods = new Dictionary<string, ProfilePeriod>();

        public ProfilePeriod[] Sum(TemplateFundingLine fundingLine, IDictionary<uint, FundingLine> fundingLines)
        {
            AggregateFundingLines(fundingLine, fundingLines);

            return _aggregatePeriods.Values.ToArray();
        }

        public void AggregateFundingLines(TemplateFundingLine templateFundingLine, IDictionary<uint, FundingLine> fundingLines)
        {
            IEnumerable<TemplateFundingLine> templateFundingLines = templateFundingLine.FundingLines?.Where(_ => _.Type == Common.TemplateMetadata.Enums.FundingLineType.Payment && fundingLines.ContainsKey(_.TemplateLineId) && fundingLines[_.TemplateLineId].Value.HasValue);

            IEnumerable<IGrouping<string, ProfilePeriod>> profilePeriodsGroupings = templateFundingLines?.Select(_ => fundingLines[_.TemplateLineId])
                .Where(_ => _.DistributionPeriods.AnyWithNullCheck())
                .SelectMany(_ => _.DistributionPeriods)
                .Where(_ => _.ProfilePeriods.AnyWithNullCheck())
                .SelectMany(_ => _.ProfilePeriods)
                .GroupBy(_ => _.DistributionPeriodId + _.TypeValue + _.Occurrence + _.Year + _.Type);

            if (profilePeriodsGroupings.AnyWithNullCheck())
            {
                foreach (IGrouping<string, ProfilePeriod> profilePeriodGrouping in profilePeriodsGroupings)
                {
                    ProfilePeriod profilePeriod;

                    if (_aggregatePeriods.ContainsKey(profilePeriodGrouping.Key))
                    {
                        profilePeriod = _aggregatePeriods[profilePeriodGrouping.Key];
                    }
                    else
                    {
                        profilePeriod = profilePeriodGrouping.First().DeepCopy();
                        profilePeriod.ProfiledValue = 0;
                        _aggregatePeriods[profilePeriodGrouping.Key] = profilePeriod;
                    }

                    profilePeriod.ProfiledValue = profilePeriod.ProfiledValue + profilePeriodGrouping.Sum(_ => _.ProfiledValue);
                }
            }

            if (templateFundingLine.FundingLines.AnyWithNullCheck())
            {
                foreach (TemplateFundingLine fundingLine in templateFundingLine.FundingLines.Where(_ => _.Type != Common.TemplateMetadata.Enums.FundingLineType.Payment))
                {
                    AggregateFundingLines(fundingLine, fundingLines);
                }
            }
        }
    }
}
