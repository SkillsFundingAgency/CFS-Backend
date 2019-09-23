using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Results.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ISpecificationsRepository
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);

        Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId);

        Task<HttpStatusCode> UpdatePublishedRefreshedDate(string specificationId, DateTimeOffset publishedRefreshDate);

        Task<IEnumerable<SpecificationSummary>> GetSpecificationSummaries();
    }
}
