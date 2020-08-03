using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderErrorDetection
    {
        void PreparePublishedProviders(IEnumerable<Provider> scopedProviders, string specificationId, string providerVersionId, FundingConfiguration fundingConfiguration);

        Task ProcessPublishedProvider(PublishedProvider publishedProvider, Func<IDetectPublishedProviderErrors, bool> predicate);

        Task ProcessPublishedProvider(PublishedProvider publishedProvider);
    }
}