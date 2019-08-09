using System;
using System.Collections.Generic;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataPopulator : IPublishedProviderDataPopulator
    {
        public bool UpdateCalculations(PublishedProvider publishedProvider, TemplateMetadataContents templateMetadataContents, IEnumerable<CalculationResult> calculationResults)
        {
            throw new NotImplementedException();
        }

        public bool UpdateFundingLines(PublishedProvider publishedProvider, IEnumerable<Models.Publishing.FundingLine> fundingLines)
        {
            throw new NotImplementedException();
        }

        public bool UpdateProfiling(PublishedProvider value, IEnumerable<Models.Publishing.FundingLine> enumerable)
        {
            throw new NotImplementedException();
        }

        public bool UpdateProviderInformation(PublishedProvider value, Common.ApiClient.Providers.Models.Provider provider)
        {
            throw new NotImplementedException();
        }
    }
}
