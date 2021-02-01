using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.Funding;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using CalculationType = CalculateFunding.Generators.Funding.Enums.CalculationType;
using FundingLineType = CalculateFunding.Generators.Funding.Enums.FundingLineType;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using ProfilePeriodType = CalculateFunding.Generators.Funding.Enums.ProfilePeriodType;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineTotalAggregator : IFundingLineTotalAggregator
    {
        private readonly FundingGenerator _fundingGenerator;
        private readonly IFundingLineRoundingSettings _fundingLineRoundingSettings;

        public FundingLineTotalAggregator(IFundingLineRoundingSettings fundingLineRoundingSettings)
        {
            Guard.ArgumentNotNull(fundingLineRoundingSettings, nameof(fundingLineRoundingSettings));

            _fundingGenerator = new FundingGenerator();
            _fundingLineRoundingSettings = fundingLineRoundingSettings;
        }

        public GeneratorModels.FundingValue GenerateTotals(TemplateModels.TemplateMetadataContents templateMetadataContents,
            IDictionary<uint, TemplateMappingItem> mappingItems,
            IDictionary<string, CalculationResult> calculationResults)
        {
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));
            Guard.ArgumentNotNull(mappingItems, nameof(mappingItems));
            Guard.ArgumentNotNull(calculationResults, nameof(calculationResults));

            IEnumerable<GeneratorModels.FundingLine> fundingLines = GetFundingLines(templateMetadataContents.RootFundingLines, mappingItems, calculationResults);

            return _fundingGenerator.GenerateFundingValue(fundingLines, _fundingLineRoundingSettings.DecimalPlaces);
        }

        private IEnumerable<GeneratorModels.FundingLine> GetFundingLines(IEnumerable<TemplateModels.FundingLine> fundingLines,
            IDictionary<uint, TemplateMappingItem> mappingItems,
            IDictionary<string, CalculationResult> calculationResults)
        {
            return (fundingLines?.Select(funding => new GeneratorModels.FundingLine
            {
                Calculations = GetCalculations(funding.Calculations, mappingItems, calculationResults),
                DistributionPeriods = GetDistributionPeriods(funding.DistributionPeriods),
                FundingLineCode = funding.FundingLineCode,
                FundingLines = GetFundingLines(funding.FundingLines, mappingItems, calculationResults),
                Name = funding.Name,
                TemplateLineId = funding.TemplateLineId,
                Type = funding.Type.AsMatchingEnum<FundingLineType>()
            }) ?? new GeneratorModels.FundingLine[0]).ToList();
        }

        private IEnumerable<GeneratorModels.Calculation> GetCalculations(IEnumerable<TemplateModels.Calculation> calculations,
            IDictionary<uint, TemplateMappingItem> mappingItems,
            IDictionary<string, CalculationResult> calculationResults)
        {
            return calculations?.Select(calc =>
            {
                string calculationId = GetCalculationId(mappingItems, calc.TemplateCalculationId);
                CalculationResult calculationResult = calculationResults.ContainsKey(calculationId) ? calculationResults[calculationId] : null;
                decimal? calculationResultValue = calculationResult?.Value as decimal?;

                return new GeneratorModels.Calculation
                {
                    Calculations = GetCalculations(calc.Calculations, mappingItems, calculationResults),
                    Name = calc.Name,
                    TemplateCalculationId = calc.TemplateCalculationId,
                    Type = calc.Type.AsMatchingEnum<CalculationType>(),
                    Value = calculationResult?.Value,
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
                Type = profilePeriod.Type.AsMatchingEnum<ProfilePeriodType>(),
                TypeValue = profilePeriod.TypeValue,
                Year = profilePeriod.Year
            }) ?? new GeneratorModels.ProfilePeriod[0];
        }

        private string GetCalculationId(IDictionary<uint, TemplateMappingItem> mappingItems,
            uint templateId)
        {
            string calculationId = mappingItems.ContainsKey(templateId) ? mappingItems[templateId].CalculationId : null;
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                throw new InvalidOperationException($"Unable to find CalculationId for TemplateCalculationId '{templateId}' in template mapping");
            }

            return calculationId;
        }
    }
}