using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Results.Models;


namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationResultsRepository
    {
        Task<ProviderResult> GetProviderResultById(string providerResultId, string partitionKey);
        Task<ProviderResult> GetProviderResult(string providerId, string specificationId);
        Task<ProviderResult> GetProviderResultByCalculationType(string providerId, string specificationId, CalculationType calculationType);
        Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId);
        Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results);
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1);
        Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults();
        Task ProviderResultsBatchProcessing(string specificationId, Func<List<ProviderResult>, Task> processProcessProviderResultsBatch, int itemsPerPage = 1000);
        Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationIdAndProviders(IEnumerable<string> providerIds, string specificationId);
        Task DeleteCurrentProviderResults(IEnumerable<ProviderResult> providerResults);
        Task<decimal> GetCalculationResultTotalForSpecificationId(string specificationId);
        Task<ProviderResult> GetSingleProviderResultBySpecificationId(string specificationId);
        Task<bool> CheckHasNewResultsForSpecificationIdAndTimePeriod(string specificationId, DateTimeOffset dateFrom, DateTimeOffset dateTo);
        Task<bool> ProviderHasResultsBySpecificationId(string specificationId);
        Task<ProviderWithResultsForSpecifications> GetProviderWithResultsForSpecificationsByProviderId(string providerId);
        Task UpsertSpecificationWithProviderResults(params ProviderWithResultsForSpecifications[] providerWithResultsForSpecifications); 
        ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> GetProvidersWithResultsForSpecificationBySpecificationId(string specificationId);
        Task DeleteCalculationResultsBySpecificationId(string specificationId, DeletionType deletionType);
    }
}
