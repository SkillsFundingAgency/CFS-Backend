using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IInScopePublishedProviderService
    {
        Dictionary<string, PublishedProvider> GenerateMissingProviders(IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders, SpecificationSummary specification, Reference fundingStream, Dictionary<string, PublishedProvider> publishedProviders, TemplateMetadataContents templateMetadataContents);
    }
}
