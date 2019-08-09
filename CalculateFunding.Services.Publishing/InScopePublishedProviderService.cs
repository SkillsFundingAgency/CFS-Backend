using System;
using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class InScopePublishedProviderService : IInScopePublishedProviderService
    {
        public Dictionary<string, PublishedProvider> GenerateMissingProviders(IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders, SpecificationSummary specification, Reference fundingStream, Dictionary<string, PublishedProvider> publishedProviders, TemplateMetadataContents templateMetadataContents)
        {
            throw new NotImplementedException();
        }
    }
}
