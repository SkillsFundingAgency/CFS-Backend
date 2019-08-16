using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineGenerator : IFundingLineGenerator
    {
        private readonly IFundingLineTotalAggregator _fundingLineTotalAggregator;

        public FundingLineGenerator(IFundingLineTotalAggregator fundingLineTotalAggregator)
        {
            Guard.ArgumentNotNull(fundingLineTotalAggregator, nameof(fundingLineTotalAggregator));

            _fundingLineTotalAggregator = fundingLineTotalAggregator;
        }

        /// <summary>
        /// Generate Funding Lines for a funding stream for all in scope providers
        /// </summary>
        /// <param name="templateMetadata">Template Metadata</param>
        /// <param name="templateMapping">Template Mapping</param>
        /// <param name="scopedProviders">Scoped providers for a specification</param>
        /// <param name="calculationResults">Calculation Results</param>
        /// <returns></returns>
        public Dictionary<string, IEnumerable<Models.Publishing.FundingLine>> GenerateFundingLines(TemplateMetadataContents templateMetadata, Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders, IEnumerable<ProviderCalculationResult> calculationResults)
        {
            Dictionary<string, IEnumerable<Models.Publishing.FundingLine>> fundingLines = new Dictionary<string, IEnumerable<Models.Publishing.FundingLine>>();

            // TODO: Check for nulls in dictionary and new up provider and funding stream sub dictionaries
            foreach (Common.ApiClient.Providers.Models.Provider provider in scopedProviders)
            {
                ProviderCalculationResult calculationResultsForProvider = calculationResults.FirstOrDefault(p => p.ProviderId == provider.ProviderId);
                IEnumerable<Models.Publishing.FundingLine> fundingLinesTotals = _fundingLineTotalAggregator.GenerateTotals(templateMetadata, calculationResultsForProvider.Results);

                fundingLines[provider.ProviderId] = fundingLinesTotals;
            }

            return fundingLines;
        }
    }
}
