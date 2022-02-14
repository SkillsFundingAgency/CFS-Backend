using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedProviderContentChannelPersistenceService
    {
        Task SavePublishedProviderContents(
            SpecificationSummary specification,
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            Channel channel,
            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders);

        Task SavePublishedProviderContents(
            Common.ApiClient.Calcs.Models.TemplateMapping templateMapping, 
            IEnumerable<PublishedProviderVersion> publishedProviderVersions, 
            Channel channel);
    }
}
