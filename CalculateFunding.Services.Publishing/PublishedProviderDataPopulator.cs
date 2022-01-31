using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
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
        /// <param name="isNewProvider">flag indicating whether this a new provider</param>
        /// <param name="reProfileAudits">Re-profile audits to check against</param>
        /// <returns>True when the PublishedProviderVersion has been updated, false if not</returns>
        public (bool changed, IEnumerable<string> variances) UpdatePublishedProvider(PublishedProviderVersion publishedProviderVersion,
            GeneratedProviderResult generatedProviderResult,
            Provider provider,
            string templateVersion,
            bool isNewProvider,
            IEnumerable<ReProfileAudit> reProfileAudits)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(generatedProviderResult, nameof(generatedProviderResult));
            Guard.ArgumentNotNull(provider, nameof(provider));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            Provider mappedProvider = _providerMapper.Map<Provider>(provider);

            PublishedProviderVersionComparer publishedProviderVersionComparer = new PublishedProviderVersionComparer();

            // if this is a new provider then it will always need to be updated
            bool equal = !isNewProvider;
            IEnumerable<string> variances = ArraySegment<string>.Empty;

            PublishedProviderVersion providerVersionGenerated = new PublishedProviderVersion
            {
                TemplateVersion = templateVersion,
                Provider = mappedProvider,
                ReProfileAudits = reProfileAudits
            };

            // if there are no calculations to action then there is nothing to compare against or to override
            if (generatedProviderResult.HasCalculations)
            {
                providerVersionGenerated.FundingLines = generatedProviderResult.FundingLines;
                providerVersionGenerated.Calculations = generatedProviderResult.Calculations;
                providerVersionGenerated.ReferenceData = generatedProviderResult.ReferenceData;
            }

            if (equal)
            {
                equal = publishedProviderVersionComparer.Equals(publishedProviderVersion, providerVersionGenerated);

                if (!equal)
                {
                    variances = publishedProviderVersionComparer.Variances;
                    _logger.Information($"changes for published provider version : {publishedProviderVersion.Id} : {publishedProviderVersionComparer.Variances.AsJson()}");
                }
            }

            if (generatedProviderResult.HasCalculations)
            {
                publishedProviderVersion.FundingLines = generatedProviderResult.FundingLines;

                publishedProviderVersion.Calculations = generatedProviderResult.Calculations;

                publishedProviderVersion.ReferenceData = generatedProviderResult.ReferenceData;

                publishedProviderVersion.TotalFunding = generatedProviderResult.TotalFunding;
            }

            publishedProviderVersion.TemplateVersion = templateVersion;

            publishedProviderVersion.Provider = mappedProvider;

            return (!equal, variances);
        }
    }
}
