using System.Collections.Generic;
using System.Dynamic;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderStateSummaryCsvTransform
    {
        IEnumerable<ExpandoObject> Transform(FundingConfiguration fundingConfiguration, IDictionary<string, PublishedProvider> publishedProviders,
                                                IDictionary<string, IEnumerable<ReleaseChannel>> releaseChannelLookupByProviderId);
        bool IsForJobDefinition(string jobDefinitionName);
    }
}
