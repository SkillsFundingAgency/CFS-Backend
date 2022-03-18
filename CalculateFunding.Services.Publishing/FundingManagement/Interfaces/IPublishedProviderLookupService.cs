using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedProviderLookupService
    {
        Task<IEnumerable<string>> GetEligibleProvidersToApproveAndRelease(string specificationId);
        Task<IEnumerable<PublishedProviderFundingSummary>> GetPublishedProviderFundingSummaries(
            SpecificationSummary specificationSummary,
            PublishedProviderStatus[] statuses,
            IEnumerable<string> publishedProviderIds = null
        );
    }
}
