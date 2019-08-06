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

        public Dictionary<string, IEnumerable<Models.Publishing.FundingLine>> GenerateFundingLines(TemplateMetadataContents templateMetadata, IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders, IEnumerable<ProviderResult> calculationResults)
        {
            Dictionary<string, IEnumerable<Models.Publishing.FundingLine>> fundingLines = new Dictionary<string, IEnumerable<Models.Publishing.FundingLine>>();

            // TODO: Check for nulls in dictionary and new up provider and funding stream sub dictionaries
            foreach (Common.ApiClient.Providers.Models.Provider provider in scopedProviders)
            {
                ProviderResult calculationResultsForProvider = calculationResults.FirstOrDefault(p => p.ProviderId == provider.ProviderId);
                IEnumerable<Models.Publishing.FundingLine> fundingLinesTotals = _fundingLineTotalAggregator.GenerateTotals(templateMetadata, calculationResultsForProvider.Results);

                fundingLines[provider.ProviderId] = fundingLinesTotals;
            }

            return fundingLines;
        }
    }
}
