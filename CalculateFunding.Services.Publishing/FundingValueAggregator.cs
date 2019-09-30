using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class FundingValueAggregator
    {
        private Dictionary<uint, dynamic> aggregatedCalculations;
        private Dictionary<uint, decimal> aggregatedFundingLines;
        private Dictionary<string, decimal> aggregatedDistributionPeriods;

        public FundingValueAggregator()
        {
            aggregatedCalculations = new Dictionary<uint, dynamic>();
            aggregatedFundingLines = new Dictionary<uint, decimal>();
            aggregatedDistributionPeriods = new Dictionary<string, decimal>();
        }

        public IEnumerable<AggregateFundingLine> GetTotals(TemplateMetadataContents templateMetadataContent, IEnumerable<PublishedProviderVersion> publishedProviders)
        {
            publishedProviders?.ToList().ForEach(provider =>
            {
                Dictionary<uint, decimal> calculations = new Dictionary<uint, decimal>();

                provider.Calculations?.ToList().ForEach(calculation => GetCalculation(calculations, calculation));

                Dictionary<uint, decimal> fundingLines = new Dictionary<uint, decimal>();

                provider.FundingLines?.ToList().ForEach(fundingLine => GetFundigLine(fundingLines, fundingLine));
            });

            return templateMetadataContent.RootFundingLines?.Select(fundingLine => GetAggregateFundingLine(fundingLine));
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

        public void GetFundigLine(Dictionary<uint, decimal> fundingLines, Models.Publishing.FundingLine fundingLine)
        {
            // if the calculation for the current provider has not been added to the aggregated total then add it only once.
            if (fundingLines.TryAdd(fundingLine.TemplateLineId, fundingLine.Value))
            {
                AggregateFundingLine(fundingLine.TemplateLineId, fundingLine.Value);

                fundingLine.DistributionPeriods?.ToList().ForEach(_ => AggregateDistributionPeriod(_));
            }
        }

        public void AggregateFundingLine(uint key, decimal value)
        {
            if (aggregatedFundingLines.TryGetValue(key, out decimal total))
            {
                // aggregate the value
                aggregatedFundingLines[key] = total + value;
            }
            else
            {
                aggregatedFundingLines.Add(key, value);
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
        }

        public void AggregateCalculation(uint key, decimal value)
        {
            if (aggregatedCalculations.TryGetValue(key, out dynamic aggregate))
            {
                // aggregate the value
                aggregatedCalculations[key] = new { total = aggregate.total + value, items = aggregate.items + 1 };
            }
            else
            {
                aggregatedCalculations.Add(key, new { total = value, items = 1 });
            }
        }

        public AggregateFundingCalculation GetAggregateCalculation(Common.TemplateMetadata.Models.Calculation calculation)
        {
            if (aggregatedCalculations.TryGetValue(calculation.TemplateCalculationId, out dynamic aggregate))
            {
                decimal aggregateValue = 0;

                switch (calculation.AggregationType)
                {
                    case Common.TemplateMetadata.Enums.AggregationType.Average:
                        {
                            aggregateValue = aggregate.items == 0 ? 0 : aggregate.total / aggregate.items;
                            break;
                        }
                    case Common.TemplateMetadata.Enums.AggregationType.Sum:
                        {
                            aggregateValue = aggregate.total;
                            break;
                        }
                }

                return new AggregateFundingCalculation
                {
                    TemplateCalculationId = calculation.TemplateCalculationId,
                    Value = aggregateValue,
                    Calculations = calculation.Calculations?.Select(x => GetAggregateCalculation(x)).Where(x => x != null)
                };
            }
            else
            {
                return null;
            }
        }

        public AggregateFundingLine GetAggregateFundingLine(Common.TemplateMetadata.Models.FundingLine fundingLine)
        {
            if (aggregatedFundingLines.TryGetValue(fundingLine.TemplateLineId, out decimal total))
            {
                return new AggregateFundingLine
                {
                    Name = fundingLine.Name,
                    TemplateLineId = fundingLine.TemplateLineId,
                    Calculations = fundingLine.Calculations?.Select(calculation => GetAggregateCalculation(calculation)).Where(x => x != null),
                    FundingLines = fundingLine.FundingLines?.Select(x => GetAggregateFundingLine(x)),
                    DistributionPeriods = fundingLine.DistributionPeriods?.Select(x => GetAggregatePeriods(x)),
                    Value = total
                };
            }
            else
            {
                return null;
            }
        }

        public AggregateDistributionPeriod GetAggregatePeriods(Common.TemplateMetadata.Models.DistributionPeriod period)
        {
            if (aggregatedDistributionPeriods.TryGetValue(period.DistributionPeriodId, out decimal total))
            {
                return new AggregateDistributionPeriod
                {
                    DistributionPeriodId = period.DistributionPeriodId,
                    Value = total
                };
            }
            else
            {
                return null;
            }
        }
    }
}
