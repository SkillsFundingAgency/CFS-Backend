using System.Linq;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Newtonsoft.Json;

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
        /// <param name="templateVersion">The template version used for the specification and provider</param>
        /// <param name="variationForProvider"></param>
        /// <returns>True when the PublishedProviderVersion has been updated, false if not</returns>
        public bool UpdatePublishedProvider(PublishedProviderVersion publishedProviderVersion,
            GeneratedProviderResult generatedProviderResult,
            Common.ApiClient.Providers.Models.Provider provider,
            string templateVersion,
            ProviderVariationResult variationForProvider)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(generatedProviderResult, nameof(generatedProviderResult));
            Guard.ArgumentNotNull(provider, nameof(provider));

            Provider mappedProvider = _providerMapper.Map<Provider>(provider);

            bool hasChanges = !(JsonConvert.SerializeObject(publishedProviderVersion.FundingLines) == JsonConvert.SerializeObject(generatedProviderResult.FundingLines)
                && JsonConvert.SerializeObject(publishedProviderVersion.Calculations) == JsonConvert.SerializeObject(generatedProviderResult.Calculations)
                && JsonConvert.SerializeObject(publishedProviderVersion.ReferenceData) == JsonConvert.SerializeObject(generatedProviderResult.ReferenceData)
                && publishedProviderVersion.TemplateVersion == templateVersion
                && publishedProviderVersion.TotalFunding == generatedProviderResult.TotalFunding
                && JsonConvert.SerializeObject(publishedProviderVersion.Provider) == JsonConvert.SerializeObject(mappedProvider));

            publishedProviderVersion.FundingLines = generatedProviderResult.FundingLines;

            publishedProviderVersion.Calculations = generatedProviderResult.Calculations;

            publishedProviderVersion.ReferenceData = generatedProviderResult.ReferenceData;

            publishedProviderVersion.TemplateVersion = templateVersion;

            publishedProviderVersion.TotalFunding = generatedProviderResult.TotalFunding;

            publishedProviderVersion.Provider = mappedProvider;

            publishedProviderVersion.VariationReasons = variationForProvider?.VariationReasons?.ToArray();

            return hasChanges;
        }
    }
}
