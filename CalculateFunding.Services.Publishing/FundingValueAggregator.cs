using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using AggregationType = CalculateFunding.Common.TemplateMetadata.Enums.AggregationType;
using DistributionPeriod = CalculateFunding.Models.Publishing.DistributionPeriod;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using ProfilePeriod = CalculateFunding.Models.Publishing.ProfilePeriod;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class FundingValueAggregator
    {
        private Dictionary<uint, AggregationType> _aggregationTypes;
        private Dictionary<uint, ICollection<decimal>> _rawCalculations;
        private Dictionary<uint, (decimal, int)> _aggregatedCalculations;
        private Dictionary<uint, (decimal?, IEnumerable<DistributionPeriod>)> _aggregatedFundingLines;
        private Dictionary<string, decimal> _aggregatedDistributionPeriods;
        private Dictionary<string, decimal> _aggregatedProfilePeriods;

        public IEnumerable<AggregateFundingLine> GetTotals(TemplateMetadataContents templateMetadataContent,
            IEnumerable<PublishedProviderVersion> publishedProviders)
        {
            _aggregatedCalculations = new Dictionary<uint, (decimal, int)>();
            _aggregatedFundingLines = new Dictionary<uint, (decimal?, IEnumerable<DistributionPeriod>)>();
            _aggregatedDistributionPeriods = new Dictionary<string, decimal>();
            _aggregatedProfilePeriods = new Dictionary<string, decimal>();
            _rawCalculations = new Dictionary<uint, ICollection<decimal>>();
            _aggregationTypes = new Dictionary<uint, AggregationType>();

            foreach (PublishedProviderVersion provider in publishedProviders)
            {
                Dictionary<uint, decimal> calculations = new Dictionary<uint, decimal>();

                foreach (FundingCalculation calculation in provider.Calculations ?? ArraySegment<FundingCalculation>.Empty)
                {
                    GetCalculation(calculations, calculation);
                }

                Dictionary<uint, decimal?> fundingLines = new Dictionary<uint, decimal?>();

                foreach (FundingLine fundingLine in provider.FundingLines ?? ArraySegment<FundingLine>.Empty)
                {
                    GetFundingLine(fundingLines, fundingLine);
                }
            }

            AggregateFundingLine[] aggregateFundingLines = templateMetadataContent.RootFundingLines?.Select(GetAggregateFundingLine).ToArray();

            ProcessSecondPassAggregations(templateMetadataContent.RootFundingLines, aggregateFundingLines);

            return aggregateFundingLines;
        }

        private void ProcessSecondPassAggregations(IEnumerable<TemplateFundingLine> fundingLines,
            IEnumerable<AggregateFundingLine> aggregateFundingLines)
        {
            IEnumerable<AggregateFundingLine> flattenedFundingLines = aggregateFundingLines.Flatten(_ => _.FundingLines);
            IDictionary<uint, AggregateFundingCalculation[]> flattenedAggregateFundingCalculations =
                flattenedFundingLines.SelectMany(_ => _.Calculations.Flatten(calc => calc.Calculations))
                    .GroupBy(_ => _.TemplateCalculationId)
                    .ToDictionary(_ => _.Key, _ => _.ToArray());

            ProcessSecondPassAggregationsForAggregateType(fundingLines, flattenedAggregateFundingCalculations, AggregationType.GroupRate);
            ProcessSecondPassAggregationsForAggregateType(fundingLines, flattenedAggregateFundingCalculations, AggregationType.PercentageChangeBetweenAandB);
        }

        private void ProcessSecondPassAggregationsForAggregateType(IEnumerable<TemplateFundingLine> fundingLines,
            IDictionary<uint, AggregateFundingCalculation[]> flattenedAggregateFundingCalculations,
            AggregationType aggregationType)
        {
            foreach (TemplateFundingLine fundingLine in fundingLines)
            {
                SetSecondPassAggregateCalculationValuesForFundingLine(fundingLine,
                    flattenedAggregateFundingCalculations,
                    aggregationType);
            }
        }

        private void SetSecondPassAggregateCalculationValuesForFundingLine(TemplateFundingLine templateFundingLine,
            IDictionary<uint, AggregateFundingCalculation[]> flattenedAggregateCalculations,
            AggregationType aggregationType)
        {
            foreach (TemplateFundingLine nestedTemplateFundingLine in templateFundingLine.FundingLines ?? ArraySegment<TemplateFundingLine>.Empty)
            {
                SetSecondPassAggregateCalculationValuesForFundingLine(nestedTemplateFundingLine,
                    flattenedAggregateCalculations,
                    aggregationType);
            }

            foreach (Calculation templateCalculation in templateFundingLine.Calculations ?? ArraySegment<Calculation>.Empty)
            {
                SetSecondPassAggregateCalculationValuesForCalculation(templateCalculation,
                    flattenedAggregateCalculations,
                    aggregationType);
            }
        }

        private void SetSecondPassAggregateCalculationValuesForCalculation(Calculation templateCalculation,
            IDictionary<uint, AggregateFundingCalculation[]> flattenedAggregateCalculations,
            AggregationType aggregationType)
        {
            if (templateCalculation.AggregationType == aggregationType)
            {
                uint templateCalculationId = templateCalculation.TemplateCalculationId;

                if (!flattenedAggregateCalculations.TryGetValue(templateCalculationId, out AggregateFundingCalculation[] aggregateFundingCalculations))
                {
                    throw new ArgumentOutOfRangeException(nameof(templateCalculationId),
                        $"Did not locate an aggregate funding calculation for template calculation id {templateCalculationId}");
                }

                SetSecondPassAggregateCalculationsValue(templateCalculation, aggregateFundingCalculations);
            }

            foreach (Calculation nestedTemplateCalculation in templateCalculation.Calculations ?? ArraySegment<Calculation>.Empty)
            {
                SetSecondPassAggregateCalculationValuesForCalculation(nestedTemplateCalculation,
                    flattenedAggregateCalculations,
                    aggregationType);
            }
        }

        private void SetSecondPassAggregateCalculationsValue(Calculation templateCalculation,
            AggregateFundingCalculation[] aggregateFundingCalculations)
        {
            decimal? value = templateCalculation.AggregationType switch
            {
                AggregationType.PercentageChangeBetweenAandB => GetPercentageChangeBetweenAandB(templateCalculation),
                AggregationType.GroupRate => GetGroupRate(templateCalculation),
                _ => throw new NotSupportedException()
            };

            foreach (AggregateFundingCalculation aggregateFundingCalculation in aggregateFundingCalculations)
            {
                aggregateFundingCalculation.Value = value;
            }
        }

        private decimal? GetGroupRate(Calculation templateCalculation)
        {
            GroupRate groupRate = templateCalculation.GroupRate;

            uint numeratorCalculation = groupRate.Numerator;
            uint denominatorCalculation = groupRate.Denominator;

            AggregationType sum = AggregationType.Sum;
            
            EnsureGroupRateCalculationComponentIsSummed(numeratorCalculation);
            EnsureGroupRateCalculationComponentIsSummed(denominatorCalculation);

            decimal? numerator = CalculateAggregateValueFor(numeratorCalculation, sum);
            decimal? denominator = CalculateAggregateValueFor(denominatorCalculation, sum);

            if (numerator.HasValue && denominator.HasValue)
            {
                decimal? rate = denominator == 0 ? 0 : numerator / denominator;

                UpdateRawCalculation(templateCalculation.TemplateCalculationId, new[] { rate.Value });

                return rate;
            }
            else
            {
                return null;
            }
        }

        private void EnsureGroupRateCalculationComponentIsSummed(uint numeratorCalculation)
        {
            if (!AggregationTypeIs(numeratorCalculation, AggregationType.Sum))
            {
                throw new InvalidOperationException(
                    $"Group rate aggregations can only be used on other calculations when they are Sum aggregations. {numeratorCalculation} is not a Sum aggregation");
            }
        }

        private decimal? GetPercentageChangeBetweenAandB(Calculation templateCalculation)
        {
            PercentageChangeBetweenAandB percentageChangeBetweenAandB = templateCalculation.PercentageChangeBetweenAandB;
            
            uint calculationA = percentageChangeBetweenAandB.CalculationA;
            uint calculationB = percentageChangeBetweenAandB.CalculationB;

            //TODO: this is set by the template and it should be set correctly for GroupRate as a quick fix 
            //      we will get it from calculation A.
            //AggregationType aggregationType = percentageChangeBetweenAandB.CalculationAggregationType;

            AggregationType aggregationType = GetAggregationType(calculationA);

            decimal? valueA = CalculateAggregateValueFor(calculationA, aggregationType);
            decimal? valueB = CalculateAggregateValueFor(calculationB, aggregationType);

            if (valueA.HasValue && valueB.HasValue)
            {
                return valueA == 0 ? 0 : (valueB - valueA) / valueA * 100;
            }
            else
            {
                return null;
            }
        }

        private bool AggregationTypeIs(uint templateCalculationId,
            params AggregationType[] permittedAggregationTypes)
            => permittedAggregationTypes.Contains(GetAggregationType(templateCalculationId));

        private decimal? CalculateAggregateValueFor(uint templateCalculationId,
            AggregationType aggregationType)
        {
            if (!_rawCalculations.TryGetValue(templateCalculationId, out ICollection<decimal> providerValues))
            {
                return null;
            }

            return aggregationType switch
            {
                AggregationType.Average => providerValues.Average(),
                AggregationType.Sum => providerValues.Sum(),
                AggregationType.GroupRate => providerValues.First(),
                _ => throw new ArgumentOutOfRangeException(nameof(AggregationType))
            };
        }


        private void GetCalculation(Dictionary<uint, decimal> calculations,
            FundingCalculation calculation)
        {
            if (decimal.TryParse(calculation.Value?.ToString(), out decimal value))
            {
                uint templateCalculationId = calculation.TemplateCalculationId;

                // if the calculation for the current provider has not been added to the aggregated total then add it only once.
                if (calculations.TryAdd(templateCalculationId, value))
                {
                    AggregateCalculation(templateCalculationId, value);
                    AddRawCalculation(templateCalculationId, value);
                }
            }
        }

        private void GetFundingLine(Dictionary<uint, decimal?> fundingLines,
            FundingLine fundingLine)
        {
            // if the calculation for the current provider has not been added to the aggregated total then add it only once.
            if (fundingLines.TryAdd(fundingLine.TemplateLineId, fundingLine.Value))
            {
                AggregateFundingLine(fundingLine.TemplateLineId, fundingLine.Value, fundingLine.DistributionPeriods);

                foreach (DistributionPeriod distributionPeriod in fundingLine.DistributionPeriods?.Where(_ => _ != null) ?? ArraySegment<DistributionPeriod>.Empty)
                {
                    AggregateDistributionPeriod(distributionPeriod, fundingLine.TemplateLineId);
                }
            }
        }

        private void AggregateFundingLine(uint key,
            decimal? value,
            IEnumerable<DistributionPeriod> distributionPeriods)
        {
            if (_aggregatedFundingLines.TryGetValue(key, out (decimal? Total, IEnumerable<DistributionPeriod> DistributionPeriods) aggregate))
            {
                aggregate = (aggregate.Total + value, aggregate.DistributionPeriods);
                // aggregate the value
                _aggregatedFundingLines[key] = aggregate;
            }
            else
            {
                _aggregatedFundingLines.Add(key, (value, distributionPeriods));
            }
        }

        private void AggregateDistributionPeriod(DistributionPeriod distributionPeriod, uint fundingLineId)
        {
            string uniqueDistributedProfileKey = $"{fundingLineId}-{distributionPeriod.DistributionPeriodId}";
            if (_aggregatedDistributionPeriods.TryGetValue(uniqueDistributedProfileKey, out decimal total))
            {
                // aggregate the value
                _aggregatedDistributionPeriods[uniqueDistributedProfileKey] = total + distributionPeriod.Value;
            }
            else
            {
                _aggregatedDistributionPeriods.Add(uniqueDistributedProfileKey, distributionPeriod.Value);
            }

            foreach (ProfilePeriod profilePeriod in distributionPeriod.ProfilePeriods?.Where(_ => _ != null) ?? ArraySegment<ProfilePeriod>.Empty)
            {
                AggregateProfilePeriod(profilePeriod, uniqueDistributedProfileKey);
            }
        }

        private void AggregateProfilePeriod(ProfilePeriod profilePeriod, string uniqueDistributedProfileKey)
        {
            string uniqueProfilePeriodKey = $"{uniqueDistributedProfileKey}-{profilePeriod.DistributionPeriodId}-{profilePeriod.TypeValue}-{profilePeriod.Year}-{profilePeriod.Occurrence}";
            if (_aggregatedProfilePeriods.TryGetValue(uniqueProfilePeriodKey, out decimal total))
            {
                // aggregate the value
                _aggregatedProfilePeriods[uniqueProfilePeriodKey] = total + profilePeriod.ProfiledValue;
            }
            else
            {
                _aggregatedProfilePeriods.Add(uniqueProfilePeriodKey, profilePeriod.ProfiledValue);
            }
        }

        private void AggregateCalculation(uint key,
            decimal value)
        {
            if (_aggregatedCalculations.TryGetValue(key, out (decimal Total, int Count) aggregate))
            {
                // aggregate the value
                _aggregatedCalculations[key] = (aggregate.Total + value, aggregate.Count + 1);
            }
            else
            {
                _aggregatedCalculations.Add(key, (value, 1));
            }
        }

        private void AddCalculationAggregationType(uint key,
            AggregationType aggregationType)
        {
            _aggregationTypes[key] = aggregationType;
        }

        private AggregationType GetAggregationType(uint templateCalculationId)
            => _aggregationTypes.TryGetValue(templateCalculationId, out AggregationType aggregationType)
                ? aggregationType
                : throw new ArgumentOutOfRangeException(nameof(templateCalculationId),
                    $"No aggregation type tracked for template calculation id {templateCalculationId}");
        
        private void AddRawCalculation(uint key,
            decimal value)
        {
            //we need the distinct list of provider values per calc for second pass as we
            //need a custom aggregation on these to form the parts of some second pass aggregations
            if (_rawCalculations.TryGetValue(key, out ICollection<decimal> providerValues))
            {
                providerValues.Add(value);
            }
            else
            {
                _rawCalculations.Add(key,
                    new List<decimal>
                    {
                        value
                    });
            }
        }

        private void UpdateRawCalculation(uint key,
            ICollection<decimal> values)
        {
            _rawCalculations[key] = values;
        }

        private AggregateFundingCalculation GetAggregateCalculation(Calculation calculation)
        {
            AddCalculationAggregationType(calculation.TemplateCalculationId, calculation.AggregationType);
            
            // make sure there this or one if it's children has an aggregation type of Sum or Average
            if (TryGetAggregateCalculation(calculation.TemplateCalculationId, out (decimal Total, int Count) aggregate) && ShouldIncludeCalculation(calculation))
            {
                decimal aggregateValue = 0;

                switch (calculation.AggregationType)
                {
                    case AggregationType.Average:
                    {
                        aggregateValue = aggregate.Count == 0 ? 0 : aggregate.Total / aggregate.Count;
                        break;
                    }
                    case AggregationType.Sum:
                    {
                        aggregateValue = aggregate.Total;
                        break;
                    }
                }

                return GetAggregateCalculationForAggregatorType(calculation, aggregateValue);
            }

            if (IsSecondPassAggregation(calculation.AggregationType))
            {
                return GetAggregateCalculationForAggregatorType(calculation, 0);
            }

            return GetAggregateCalculationForAggregatorType(calculation, null);
        }

        private bool IsSecondPassAggregation(AggregationType aggregationType)
            => aggregationType == AggregationType.GroupRate ||
               aggregationType == AggregationType.PercentageChangeBetweenAandB;

        private bool TryGetAggregateCalculation(uint templateCalculationId,
            out (decimal Total, int Count) aggregate) =>
            _aggregatedCalculations.TryGetValue(templateCalculationId, out aggregate);

        private AggregateFundingCalculation GetAggregateCalculationForAggregatorType(Calculation calculation,
            object aggregateValue)
        {
            return new AggregateFundingCalculation
            {
                TemplateCalculationId = calculation.TemplateCalculationId,
                Value = aggregateValue,
                Calculations = calculation.Calculations?.Select(GetAggregateCalculation).Where(_ => _ != null).ToArray()
            };
        }

        private bool ShouldIncludeCalculation(Calculation calculation)
        {
            if (calculation.AggregationType == AggregationType.Sum || calculation.AggregationType == AggregationType.Average)
            {
                return true;
            }

            return calculation.Calculations.AnyWithNullCheck(ShouldIncludeCalculation);
        }

        private AggregateFundingLine GetAggregateFundingLine(TemplateFundingLine fundingLine)
        {
            if (_aggregatedFundingLines.TryGetValue(fundingLine.TemplateLineId, out (decimal? Total, IEnumerable<DistributionPeriod> DistributionPeriods) aggregate))
            {
                return new AggregateFundingLine
                {
                    Name = fundingLine.Name,
                    TemplateLineId = fundingLine.TemplateLineId,
                    Calculations = fundingLine.Calculations?.Select(GetAggregateCalculation).Where(_ => _ != null).ToArray(),
                    FundingLines = fundingLine.FundingLines?.Select(GetAggregateFundingLine).ToArray(),
                    DistributionPeriods = aggregate.DistributionPeriods?.Select(_ => GetAggregatePeriods(_, fundingLine.TemplateLineId)).ToArray(),
                    Value = aggregate.Total
                };
            }

            return null;
        }

        private DistributionPeriod GetAggregatePeriods(DistributionPeriod distributionPeriod, uint key)
        {
            string uniqueDistributedProfileKey = $"{key}-{distributionPeriod.DistributionPeriodId}";
            if (distributionPeriod != null && _aggregatedDistributionPeriods.TryGetValue(uniqueDistributedProfileKey, out decimal total))
            {
                DistributionPeriod localDistributionPeriod = distributionPeriod.Clone();

                localDistributionPeriod.Value = total;

                foreach (ProfilePeriod profilePeriod in localDistributionPeriod.ProfilePeriods)
                {
                    SetAggregateProfilePeriods(profilePeriod, uniqueDistributedProfileKey);
                }

                return localDistributionPeriod;
            }

            return null;
        }

        private void SetAggregateProfilePeriods(ProfilePeriod profilePeriod, string key)
        {
            string uniqueProfilePeriodKey = $"{key}-{profilePeriod.DistributionPeriodId}-{profilePeriod.TypeValue}-{profilePeriod.Year}-{profilePeriod.Occurrence}";

            if (_aggregatedProfilePeriods.TryGetValue(uniqueProfilePeriodKey, out decimal total))
            {
                profilePeriod.ProfiledValue = total;
            }
        }
    }
}