using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Results.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ISpecificationsRepository
    {
        Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId);

        Task<HttpStatusCode> UpdatePublishedRefreshedDate(string specificationId, DateTimeOffset publishedRefreshDate);
    }
}
