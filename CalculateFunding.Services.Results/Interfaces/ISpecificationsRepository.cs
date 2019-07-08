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

        Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId);

        Task<HttpStatusCode> UpdatePublishedRefreshedDate(string specificationId, DateTimeOffset publishedRefreshDate);

        Task<IEnumerable<SpecificationSummary>> GetSpecificationSummaries();
    }
}
