using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderDataPopulator
    {
        /// <summary>
        /// Updates the given data on the Published Provider.
        /// This method is responsible for applying the data passed into on to the PublishedProviderVersion and returning if the PublishedProviderVersion has been updated
        /// </summary>
        /// <param name="publishedProviderVersion">Published Provider Version</param>
        /// <param name="generatedProviderResult">Generated funding lines, calculations and reference data</param>
        /// <param name="provider">Core provider information</param>
        /// <param name="templateVersion">The template version used for the specification and provider</param>
        /// <param name="reProfileAudits">Re-profile audits to check against</param>
        /// <returns>True when the PublishedProviderVersion has been updated, false if not</returns>
        (bool changed, IEnumerable<string> variances) UpdatePublishedProvider(PublishedProviderVersion publishedProviderVersion,
            GeneratedProviderResult generatedProviderResult,
            Provider provider,
            string templateVersion,
            bool isNewProvider,
            IEnumerable<ReProfileAudit> reProfileAudits);
    }
}
