using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class FundingValueAggregator
    {
        private Dictionary<uint, dynamic> aggregatedCalculations;

        public FundingValueAggregator()
        {
            aggregatedCalculations = new Dictionary<uint, dynamic>();
        }

        public IEnumerable<AggregateFundingLine> GetTotals(TemplateMetadataContents templateMetadataContent, IEnumerable<PublishedProviderVersion> publishedProviders)
        {
            publishedProviders?.ToList().ForEach(provider =>
            {
                Dictionary<uint, decimal> calculations = new Dictionary<uint, decimal>();

                provider.Calculations?.ToList().ForEach(calculation => GetCalculation(calculations, calculation));
            });

            return templateMetadataContent.RootFundingLines?.Select(fundingLine => ToFundingLine(fundingLine));
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

        public AggregateFundingLine ToFundingLine(Common.TemplateMetadata.Models.FundingLine fundingLine)
        {
            return new AggregateFundingLine
            {
                Name = fundingLine.Name,
                TemplateLineId = fundingLine.TemplateLineId,
                Calculations = fundingLine.Calculations?.Select(calculation => GetAggregateCalculation(calculation)).Where(x => x != null),
                FundingLines = fundingLine.FundingLines?.Select(x => ToFundingLine(x))
            };
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
    }
}
