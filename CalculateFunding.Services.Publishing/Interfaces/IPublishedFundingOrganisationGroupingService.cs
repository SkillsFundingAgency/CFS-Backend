using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingOrganisationGroupingService
    {
        Task<IEnumerable<PublishedFundingOrganisationGrouping>> GeneratePublishedFundingOrganisationGrouping(
            bool includeHistory,
            string fundingStreamId,
            SpecificationSummary specification,
            IEnumerable<PublishedFundingVersion> publishedFundingVersions);
    }
}
