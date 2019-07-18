using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationResultsRepository
    {
        Task<ProviderResult> GetProviderResult(string providerId, string specificationId);
        Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId);
        Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results);
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1);
        Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults();
        Task ProviderResultsBatchProcessing(string specificationId, Func<List<ProviderResult>, Task> persistIndexBatch);
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationIdAndProviders(IEnumerable<string> providerIds, string specificationId);
        Task DeleteCurrentProviderResults(IEnumerable<ProviderResult> providerResults);
        Task<decimal> GetCalculationResultTotalForSpecificationId(string specificationId);
        Task<ProviderResult> GetSingleProviderResultBySpecificationId(string specificationId);
        Task<bool> CheckHasNewResultsForSpecificationIdAndTimePeriod(string specificationId, DateTimeOffset dateFrom, DateTimeOffset dateTo);
    }
}
