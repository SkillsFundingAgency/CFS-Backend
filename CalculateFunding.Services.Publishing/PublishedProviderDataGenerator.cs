using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataGenerator : IPublishedProviderDataGenerator
    {
        private readonly IFundingLineTotalAggregator _fundingLineTotalAggregator;

        public PublishedProviderDataGenerator(IFundingLineTotalAggregator fundingLineTotalAggregator)
        {
            Guard.ArgumentNotNull(fundingLineTotalAggregator, nameof(fundingLineTotalAggregator));

            _fundingLineTotalAggregator = fundingLineTotalAggregator;
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
            Dictionary<string, GeneratedProviderResult> results = new Dictionary<string, GeneratedProviderResult>();

            // TODO: Check for nulls in dictionary and new up provider and funding stream sub dictionaries
            foreach (Common.ApiClient.Providers.Models.Provider provider in scopedProviders)
            {
                GeneratedProviderResult generatedProviderResult = new GeneratedProviderResult();

                ProviderCalculationResult calculationResultsForProvider = calculationResults.FirstOrDefault(p => p.ProviderId == provider.ProviderId);

                IEnumerable<Models.Publishing.FundingLine> fundingLinesTotals = _fundingLineTotalAggregator.GenerateTotals(templateMetadata, templateMapping, calculationResultsForProvider.Results);

                generatedProviderResult.FundingLines = fundingLinesTotals;

                // Generate calculations
                List<FundingCalculation> fundingCalculations = new List<FundingCalculation>();

                // Use the template to generate a single FundingCalculation per TemplateCalculationId

                // Assign the value of the calculation result to the calculation (use TemplateMaping to resolve CalculationId from TemplateCalculationId)

                generatedProviderResult.Calculations = fundingCalculations;

                // Generate reference data
                // Use the template to generate a single FundingCalculation per TemplateCalculationId
                List<FundingReferenceData> fundingReferenceData = new List<FundingReferenceData>();

                // Assign the value of the calculation result to the calculation (use TemplateMaping to resolve CalculationId from TemplateCalculationId)

                generatedProviderResult.ReferenceData = fundingReferenceData;

                // Set Provider information
                generatedProviderResult.Provider = MapProvider(provider);


                results.Add(provider.ProviderId, generatedProviderResult);
            }

            return results;
        }

        private Models.Publishing.Provider MapProvider(Common.ApiClient.Providers.Models.Provider provider)
        {
            throw new NotImplementedException();
        }
    }
}
