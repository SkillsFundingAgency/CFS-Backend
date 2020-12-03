using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.Funding;
using CalculateFunding.Services.Publishing.Interfaces;
using ApiCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineTotalAggregator : IFundingLineTotalAggregator
    {
        private readonly FundingGenerator _fundingGenerator;

        public FundingLineTotalAggregator()
        {
            _fundingGenerator = new FundingGenerator();
        }

        public GeneratorModels.FundingValue GenerateTotals(TemplateModels.TemplateMetadataContents templateMetadataContents, IDictionary<uint, TemplateMappingItem> mappingItems, IDictionary<string, CalculationResult> calculationResults)
        {
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));
            Guard.ArgumentNotNull(mappingItems, nameof(mappingItems));
            Guard.ArgumentNotNull(calculationResults, nameof(calculationResults));

            IEnumerable<GeneratorModels.FundingLine> fundingLines = GetFundingLines(templateMetadataContents.RootFundingLines, mappingItems, calculationResults);

            return _fundingGenerator.GenerateFundingValue(fundingLines);
        }

        private IEnumerable<GeneratorModels.FundingLine> GetFundingLines(IEnumerable<TemplateModels.FundingLine> fundingLines, IDictionary<uint, TemplateMappingItem> mappingItems, IDictionary<string, CalculationResult> calculationResults)
        {
            return (fundingLines?.Select(funding => new GeneratorModels.FundingLine
            {
                Calculations = GetCalculations(funding.Calculations, mappingItems, calculationResults),
                DistributionPeriods = GetDistributionPeriods(funding.DistributionPeriods),
                FundingLineCode = funding.FundingLineCode,
                FundingLines = GetFundingLines(funding.FundingLines, mappingItems, calculationResults),
                Name = funding.Name,
                TemplateLineId = funding.TemplateLineId,
                Type = funding.Type.AsMatchingEnum<Generators.Funding.Enums.FundingLineType>(),
                Value = funding.Value
            }) ?? new GeneratorModels.FundingLine[0]).ToList();
        }

        private IEnumerable<GeneratorModels.Calculation> GetCalculations(IEnumerable<TemplateModels.Calculation> calculations, IDictionary<uint, TemplateMappingItem> mappingItems, IDictionary<string, CalculationResult> calculationResults)
        {
            return calculations?.Select(calc =>
            {
                CalculationResult calculationResult = calculationResults[GetCalculationId(mappingItems, calc.TemplateCalculationId)];
                decimal? calculationResultValue = calculationResult?.Value as decimal?;

                return new GeneratorModels.Calculation
                {
                    Calculations = GetCalculations(calc.Calculations, mappingItems, calculationResults),
                    Name = calc.Name,
                    TemplateCalculationId = calc.TemplateCalculationId,
                    Type = calc.Type.AsMatchingEnum<Generators.Funding.Enums.CalculationType>(),
                    Value = calc.Type == ApiCalculationType.Cash && calculationResultValue.HasValue
                    ? Math.Round(calculationResultValue.Value, 2, MidpointRounding.AwayFromZero)
                    : calculationResult?.Value
                };
            }) ?? new GeneratorModels.Calculation[0];
        }

        private IEnumerable<GeneratorModels.DistributionPeriod> GetDistributionPeriods(IEnumerable<TemplateModels.DistributionPeriod> distributionPeriods)
        {
            return distributionPeriods?.Select(period => new GeneratorModels.DistributionPeriod
            {
                DistributionPeriodId = period.DistributionPeriodId,
                ProfilePeriods = GetProfilePeriods(period.ProfilePeriods),
                Value = period.Value
            }) ?? new GeneratorModels.DistributionPeriod[0];
        }

        private IEnumerable<GeneratorModels.ProfilePeriod> GetProfilePeriods(IEnumerable<TemplateModels.ProfilePeriod> profilePeriods)
        {
            return profilePeriods?.Select(profilePeriod => new GeneratorModels.ProfilePeriod
            {
                DistributionPeriodId = profilePeriod.DistributionPeriodId,
                Occurrence = profilePeriod.Occurrence,
                ProfiledValue = profilePeriod.ProfiledValue,
                Type = profilePeriod.Type.AsMatchingEnum<Generators.Funding.Enums.ProfilePeriodType>(),
                TypeValue = profilePeriod.TypeValue,
                Year = profilePeriod.Year
            }) ?? new GeneratorModels.ProfilePeriod[0];
        }

        private string GetCalculationId(IDictionary<uint, TemplateMappingItem> mappingItems, uint templateId)
        {
            string calculationId = mappingItems[templateId]?.CalculationId;
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                throw new InvalidOperationException($"Unable to find CalculationId for TemplateCalculationId '{templateId}' in template mapping");
            }

            return calculationId;
        }
    }
}
