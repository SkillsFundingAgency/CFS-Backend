using System;
using System.Collections.Generic;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CommonProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataPopulator : IPublishedProviderDataPopulator
    {
        private readonly IMapper _providerMapper;

        public PublishedProviderDataPopulator(IMapper providerMapper)
        {
            Guard.ArgumentNotNull(providerMapper, nameof(providerMapper));

            _providerMapper = providerMapper;
        }

        /// <summary>
        /// Updates the given data on the Published Provider.
        /// This method is responsible for applying the data passed into on to the PublishedProviderVersion and returning if the PublishedProviderVersion has been updated
        /// </summary>
        /// <param name="publishedProviderVersion">Published Provider Version</param>
        /// <param name="generatedProviderResult">Funding lines and profiling information, calculations, reference data</param>
        /// <param name="provider">Core provider information</param>
        /// <returns>True when the PublishedProviderVersion has been updated, false if not</returns>
        public bool UpdatePublishedProvider(PublishedProviderVersion publishedProviderVersion, GeneratedProviderResult generatedProviderResult, Common.ApiClient.Providers.Models.Provider provider)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(generatedProviderResult, nameof(generatedProviderResult));
            Guard.ArgumentNotNull(provider, nameof(provider));

            PublishedProviderVersion publishedProviderVersionCloned = publishedProviderVersion.Clone() as PublishedProviderVersion;

            publishedProviderVersion.FundingLines = generatedProviderResult.FundingLines;

            publishedProviderVersion.Calculations = generatedProviderResult.Calculations;

            publishedProviderVersion.ReferenceData = generatedProviderResult.ReferenceData;

            publishedProviderVersion.Provider = _providerMapper.Map<Provider>(provider);

            return !publishedProviderVersion.AreEqual(publishedProviderVersionCloned);
        }
    }
}
