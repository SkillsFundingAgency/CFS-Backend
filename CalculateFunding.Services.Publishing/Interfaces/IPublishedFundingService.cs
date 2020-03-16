using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingService
    {
        Task<PublishedFundingInput> GeneratePublishedFundingInput(IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
            IEnumerable<Provider> scopedProviders,
            Reference fundingStream,
            SpecificationSummary specification);
    }
}
