using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ISpecificationsRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);

        Task<IEnumerable<FundingStream>> GetFundingStreams();

        Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId);

        Task<Period> GetFundingPeriodById(string fundingPeriodId);

        Task<HttpStatusCode> UpdatePublishedRefreshedDate(string specificationId, DateTimeOffset publishedRefreshDate);
    }
}
