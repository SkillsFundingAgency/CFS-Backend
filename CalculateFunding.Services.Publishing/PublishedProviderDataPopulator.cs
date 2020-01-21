using System.Linq;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Comparers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderDataPopulator : IPublishedProviderDataPopulator
    {
        private readonly IMapper _providerMapper;
        private readonly ILogger _logger;

        public PublishedProviderDataPopulator(IMapper providerMapper, ILogger logger)
        {
            Guard.ArgumentNotNull(providerMapper, nameof(providerMapper));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _providerMapper = providerMapper;
            _logger = logger;
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
            ProviderVariationResult variationForProvider,
            bool isNewProvider)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(generatedProviderResult, nameof(generatedProviderResult));
            Guard.ArgumentNotNull(provider, nameof(provider));

            Provider mappedProvider = _providerMapper.Map<Provider>(provider);

            PublishedProviderVersionComparer publishedProviderVersionComparer = new PublishedProviderVersionComparer();

            // if this is a new provider then it will always need to be updated
            bool equal = !isNewProvider;

            if (equal)
            {
                equal = publishedProviderVersionComparer.Equals(publishedProviderVersion, new PublishedProviderVersion
                {
                    FundingLines = generatedProviderResult.FundingLines,
                    Calculations = generatedProviderResult.Calculations,
                    ReferenceData = generatedProviderResult.ReferenceData,
                    TemplateVersion = templateVersion,
                    Provider = mappedProvider
                });

                if (!equal)
                {
                    _logger.Information($"changes for new published provider version : {publishedProviderVersion.Id} : {publishedProviderVersionComparer.Variances.AsJson()}");
                }
            }

            publishedProviderVersion.FundingLines = generatedProviderResult.FundingLines;

            publishedProviderVersion.Calculations = generatedProviderResult.Calculations;

            publishedProviderVersion.ReferenceData = generatedProviderResult.ReferenceData;

            publishedProviderVersion.TemplateVersion = templateVersion;

            publishedProviderVersion.TotalFunding = generatedProviderResult.TotalFunding;

            publishedProviderVersion.Provider = mappedProvider;

            publishedProviderVersion.VariationReasons = variationForProvider?.VariationReasons?.ToArray();

            return !equal;
        }
    }
}
