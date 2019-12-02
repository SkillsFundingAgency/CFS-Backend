using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class FundingValueAggregator
    {
        private Dictionary<uint, (decimal, int)> aggregatedCalculations;
        private Dictionary<uint, (decimal?, IEnumerable<Models.Publishing.DistributionPeriod>)> aggregatedFundingLines;
        private Dictionary<string, decimal> aggregatedDistributionPeriods;
        private Dictionary<string, decimal> aggregatedProfilePeriods;

        public FundingValueAggregator()
        {
        }

        public IEnumerable<AggregateFundingLine> GetTotals(TemplateMetadataContents templateMetadataContent, IEnumerable<PublishedProviderVersion> publishedProviders)
        {
            aggregatedCalculations = new Dictionary<uint, (decimal, int)>();
            aggregatedFundingLines = new Dictionary<uint, (decimal?, IEnumerable<Models.Publishing.DistributionPeriod>)>();
            aggregatedDistributionPeriods = new Dictionary<string, decimal>();
            aggregatedProfilePeriods = new Dictionary<string, decimal>();

            publishedProviders?.ToList().ForEach(provider =>
            {
                Dictionary<uint, decimal> calculations = new Dictionary<uint, decimal>();

                provider.Calculations?.ToList().ForEach(calculation => GetCalculation(calculations, calculation));

                Dictionary<uint, decimal?> fundingLines = new Dictionary<uint, decimal?>();

                provider.FundingLines?.ToList().ForEach(fundingLine => GetFundingLine(fundingLines, fundingLine));
            });

            var result = templateMetadataContent.RootFundingLines?.Select(fundingLine => GetAggregateFundingLine(fundingLine));

            var r = new List<AggregateFundingLine>(result);

            return r;
        }

        public void GetCalculation(Dictionary<uint, decimal> calculations, FundingCalculation calculation)
        {
            if (decimal.TryParse(calculation.Value?.ToString(), out decimal value))
            {
                // if the calculation for the current provider has not been added to the aggregated total then add it only once.
                if (calculations.TryAdd(calculation.TemplateCalculationId, value))
                {
                    AggregateCalculation(calculation.TemplateCalculationId, value);
                }
            }
        }

        public void GetFundingLine(Dictionary<uint, decimal?> fundingLines, Models.Publishing.FundingLine fundingLine)
        {
            // if the calculation for the current provider has not been added to the aggregated total then add it only once.
            if (fundingLines.TryAdd(fundingLine.TemplateLineId, fundingLine.Value))
            {
                AggregateFundingLine(fundingLine.TemplateLineId, fundingLine.Value, fundingLine.DistributionPeriods);

                fundingLine.DistributionPeriods?.Where(_ => _ != null).ToList().ForEach(_ => AggregateDistributionPeriod(_));
            }
        }

        public void AggregateFundingLine(uint key, decimal? value, IEnumerable<Models.Publishing.DistributionPeriod> distributionPeriods)
        {
            if (aggregatedFundingLines.TryGetValue(key, out (decimal? Total, IEnumerable<Models.Publishing.DistributionPeriod> DistributionPeriods) aggregate))
            {
                aggregate = (aggregate.Total + value, aggregate.DistributionPeriods);
                // aggregate the value
                aggregatedFundingLines[key] = aggregate;
            }
            else
            {
                aggregatedFundingLines.Add(key, (value, distributionPeriods));
            }
        }

        public void AggregateDistributionPeriod(Models.Publishing.DistributionPeriod distributionPeriod)
        {
            if (aggregatedDistributionPeriods.TryGetValue(distributionPeriod.DistributionPeriodId, out decimal total))
            {
                // aggregate the value
                aggregatedDistributionPeriods[distributionPeriod.DistributionPeriodId] = total + distributionPeriod.Value;
            }
            else
            {
                aggregatedDistributionPeriods.Add(distributionPeriod.DistributionPeriodId, distributionPeriod.Value);
            }

            distributionPeriod.ProfilePeriods?.Where(_ => _ != null).ToList().ForEach(_ => AggregateProfilePeriod(_));
        }

        public void AggregateProfilePeriod(Models.Publishing.ProfilePeriod profilePeriod)
        {
            string uniqueProfilePeriodKey = $"{profilePeriod.DistributionPeriodId}-{profilePeriod.TypeValue}-{profilePeriod.Year}-{profilePeriod.Occurrence}";
            if (aggregatedProfilePeriods.TryGetValue(uniqueProfilePeriodKey, out decimal total))
            {
                // aggregate the value
                aggregatedProfilePeriods[uniqueProfilePeriodKey] = total + profilePeriod.ProfiledValue;
            }
            else
            {
                aggregatedProfilePeriods.Add(uniqueProfilePeriodKey, profilePeriod.ProfiledValue);
            }
        }

        public void AggregateCalculation(uint key, decimal value)
        {
            if (aggregatedCalculations.TryGetValue(key, out (decimal Total, int Count) aggregate))
            {
                // aggregate the value
                aggregatedCalculations[key] = (aggregate.Total + value, aggregate.Count + 1);
            }
            else
            {
                aggregatedCalculations.Add(key, (value, 1));
            }
        }

        public AggregateFundingCalculation GetAggregateCalculation(Common.TemplateMetadata.Models.Calculation calculation)
        {
            // make sure there this or one if it's children has an aggregation type of Sum or Average
            if (aggregatedCalculations.TryGetValue(calculation.TemplateCalculationId, out (decimal Total, int Count) aggregate) && ShouldIncludeCalculation(calculation))
            {
                decimal aggregateValue = 0;

                switch (calculation.AggregationType)
                {
                    case Common.TemplateMetadata.Enums.AggregationType.Average:
                        {
                            aggregateValue = aggregate.Count == 0 ? 0 : aggregate.Total / aggregate.Count;
                            break;
                        }
                    case Common.TemplateMetadata.Enums.AggregationType.Sum:
                        {
                            aggregateValue = aggregate.Total;
                            break;
                        }
                }

                return GetAggregateCalculationForAggregatorType(calculation, aggregateValue);
            }
            else
            {
                return null;
            }
        }

        private AggregateFundingCalculation GetAggregateCalculationForAggregatorType(Calculation calculation, decimal aggregateValue)
        {
            return new AggregateFundingCalculation
            {
                TemplateCalculationId = calculation.TemplateCalculationId,
                Value = aggregateValue,
                Calculations = calculation.Calculations?.Select(x => GetAggregateCalculation(x)).Where(x => x != null)
            };
        }

        private bool ShouldIncludeCalculation(Calculation calculation)
        {
            if (calculation.AggregationType == Common.TemplateMetadata.Enums.AggregationType.Sum || calculation.AggregationType == Common.TemplateMetadata.Enums.AggregationType.Average)
            {
                return true;
            }
            else
            {
                return calculation.Calculations.AnyWithNullCheck(_ => ShouldIncludeCalculation(_));
            }
        }

        public AggregateFundingLine GetAggregateFundingLine(Common.TemplateMetadata.Models.FundingLine fundingLine)
        {
            if (aggregatedFundingLines.TryGetValue(fundingLine.TemplateLineId, out (decimal? Total, IEnumerable<Models.Publishing.DistributionPeriod> DistributionPeriods) aggregate))
            {
                return new AggregateFundingLine
                {
                    Name = fundingLine.Name,
                    TemplateLineId = fundingLine.TemplateLineId,
                    Calculations = fundingLine.Calculations?.Select(calculation => GetAggregateCalculation(calculation)).Where(x => x != null),
                    FundingLines = fundingLine.FundingLines?.Select(x => GetAggregateFundingLine(x)),
                    DistributionPeriods = aggregate.DistributionPeriods?.Select(x => GetAggregatePeriods(x)),
                    Value = aggregate.Total
                };
            }
            else
            {
                return null;
            }
        }

        public Models.Publishing.DistributionPeriod GetAggregatePeriods(Models.Publishing.DistributionPeriod distributionPeriod)
        {
            if (distributionPeriod != null && aggregatedDistributionPeriods.TryGetValue(distributionPeriod.DistributionPeriodId, out decimal total))
            {
                Models.Publishing.DistributionPeriod localDistributionPeriod = distributionPeriod.Clone();

                localDistributionPeriod.Value = total;
                localDistributionPeriod.ProfilePeriods?.ToList().ForEach(x => SetAggregateProfilePeriods(x));
                return localDistributionPeriod;
            }
            else
            {
                return null;
            }
        }

        public void SetAggregateProfilePeriods(Models.Publishing.ProfilePeriod profilePeriod)
        {
            string uniqueProfilePeriodKey = $"{profilePeriod.DistributionPeriodId}-{profilePeriod.TypeValue}-{profilePeriod.Year}-{profilePeriod.Occurrence}";

            if (profilePeriod != null && aggregatedProfilePeriods.TryGetValue(uniqueProfilePeriodKey, out decimal total))
            {
                profilePeriod.ProfiledValue = total;
            }
        }
    }
}
