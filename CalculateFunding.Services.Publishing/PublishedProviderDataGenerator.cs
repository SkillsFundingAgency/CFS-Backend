using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using StackExchange.Redis;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataGenerator : IPublishedProviderDataGenerator
    {
        private readonly IFundingLineTotalAggregator _fundingLineTotalAggregator;
        private readonly IMapper _mapper;

        public PublishedProviderDataGenerator(IFundingLineTotalAggregator fundingLineTotalAggregator, IMapper mapper)
        {
            Guard.ArgumentNotNull(fundingLineTotalAggregator, nameof(fundingLineTotalAggregator));

            _fundingLineTotalAggregator = fundingLineTotalAggregator;
            _mapper = mapper;
        }

        /// <summary>
        /// Generate Funding Lines, Calculations and Reference Data for a funding stream for all in scope providers
        /// </summary>
        /// <param name="templateMetadata">Template Metadata</param>
        /// <param name="templateMapping">Template Mapping</param>
        /// <param name="scopedProviders">Scoped providers for a specification</param>
        /// <param name="calculationResults">Calculation Results</param>
        /// <returns>Dictionary of Generated Provider Results, keyed on ProviderId</returns>
        public Dictionary<string, GeneratedProviderResult> Generate(TemplateMetadataContents templateMetadata, Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders, IEnumerable<ProviderCalculationResult> calculationResults)
        {
            Guard.ArgumentNotNull(templateMetadata, nameof(templateMetadata));
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));
            Guard.ArgumentNotNull(calculationResults, nameof(calculationResults));

            Dictionary<string, GeneratedProviderResult> results = new Dictionary<string, GeneratedProviderResult>();

            foreach (Common.ApiClient.Providers.Models.Provider provider in scopedProviders)
            {
                GeneratedProviderResult generatedProviderResult = new GeneratedProviderResult();

                ProviderCalculationResult calculationResultsForProvider = calculationResults.FirstOrDefault(p => p.ProviderId == provider.ProviderId);

                if (calculationResultsForProvider != null)
                {
                    GeneratorModels.FundingValue fundingValue = _fundingLineTotalAggregator.GenerateTotals(templateMetadata, templateMapping, calculationResultsForProvider.Results);

                    // Get funding lines
                    IEnumerable<GeneratorModels.FundingLine> fundingLines = fundingValue.FundingLines?.Flatten(_ => _.FundingLines) ?? new GeneratorModels.FundingLine[0];

                    generatedProviderResult.FundingLines = _mapper.Map<IEnumerable<Models.Publishing.FundingLine>>(fundingLines);

                    // Get calculations
                    IEnumerable<GeneratorModels.Calculation> fundingCalculations = fundingLines?.SelectMany(_ => _.Calculations.Flatten(calc => calc.Calculations)) ?? new GeneratorModels.Calculation[0];

                    generatedProviderResult.Calculations = _mapper.Map<IEnumerable<FundingCalculation>>(fundingCalculations);

                    // Get reference data
                    IEnumerable<GeneratorModels.ReferenceData> fundingReferenceData = fundingCalculations?.SelectMany(_ => _.ReferenceData);

                    generatedProviderResult.ReferenceData = _mapper.Map<IEnumerable<FundingReferenceData>>(fundingReferenceData);

                    // Set Provider information
                    generatedProviderResult.Provider = _mapper.Map<Provider>(provider);

                    results.Add(provider.ProviderId, generatedProviderResult);
                }
            }

            return results;
        }

        private Common.TemplateMetadata.Models.FundingLine ToFundingLine(Common.TemplateMetadata.Models.FundingLine fundingLine, Common.ApiClient.Calcs.Models.TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            fundingLine.Calculations = fundingLine.Calculations?.Select(_ => ToCalculation(_, mapping, calculationResults));

            fundingLine.FundingLines = fundingLine.FundingLines?.Select(_ => ToFundingLine(_, mapping, calculationResults));

            return fundingLine;
        }

        private Common.TemplateMetadata.Models.Calculation ToCalculation(Common.TemplateMetadata.Models.Calculation calculation, Common.ApiClient.Calcs.Models.TemplateMapping mapping, IEnumerable<CalculationResult> calculationResults)
        {
            decimal? calculationResultValue = calculationResults.SingleOrDefault(calc => calc.Id == GetCalculationId(mapping, calculation.TemplateCalculationId))?.Value;
            
            calculation.Value = calculation.Type == CalculationType.Cash && calculationResultValue.HasValue 
                ? (object) Math.Round(calculationResultValue.Value, 2, MidpointRounding.AwayFromZero) 
                :  calculationResultValue;

            calculation.Calculations = calculation.Calculations?.Select(_ => ToCalculation(_, mapping, calculationResults));

            return calculation;
        }

        private string GetCalculationId(Common.ApiClient.Calcs.Models.TemplateMapping mapping, uint templateId)
        {
            return mapping.TemplateMappingItems.SingleOrDefault(_ => _.TemplateId == templateId)?.CalculationId;
        }
    }
}
