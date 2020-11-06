using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Polly;

namespace CalculateFunding.Services.CalcEngine
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly AsyncPolicy _providerSourceDatasetsRepositoryPolicy;

        public ProviderSourceDatasetsRepository(ICosmosRepository cosmosRepository,
            ICalculatorResiliencePolicies calculatorResiliencePolicies
            )
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(calculatorResiliencePolicies, nameof(calculatorResiliencePolicies));
            Guard.ArgumentNotNull(calculatorResiliencePolicies.ProviderSourceDatasetsRepository, nameof(calculatorResiliencePolicies));

            _cosmosRepository = cosmosRepository;
            _providerSourceDatasetsRepositoryPolicy = calculatorResiliencePolicies.ProviderSourceDatasetsRepository;
        }

        public async Task<Dictionary<string, Dictionary<string, ProviderSourceDataset>>> GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
            string specificationId,
            IEnumerable<string> providerIds,
            IEnumerable<string> dataRelationshipIds)
        {
            if (providerIds.IsNullOrEmpty() || dataRelationshipIds.IsNullOrEmpty())
            {
                return new Dictionary<string, Dictionary<string, ProviderSourceDataset>>();
            }

            Dictionary<string, Dictionary<string, ProviderSourceDataset>> results = new Dictionary<string, Dictionary<string, ProviderSourceDataset>>(providerIds.Count());

            List<Task<DocumentEntity<ProviderSourceDataset>>> requests = new List<Task<DocumentEntity<ProviderSourceDataset>>>(providerIds.Count() * dataRelationshipIds.Count());

            // Iterate through providers first, to give a higher possibility to batch requests based on partition ID in cosmos
            foreach (string providerId in providerIds)
            {
                EnsureResultsDictionaryContainsProvider(results, providerId);
                GenerateLookupRequestForDatasetForAProvider(specificationId, dataRelationshipIds, requests, providerId);
            }

            // No throttling required for tasks, as it's assumed the cosmos client's AllowBatch will handle the parallism
            await TaskHelper.WhenAllAndThrow(requests.ToArray());

            foreach (Task<DocumentEntity<ProviderSourceDataset>> request in requests)
            {
                DocumentEntity<ProviderSourceDataset> providerSourceDatasetDocument = request.Result;

                if (IsReturnedDocumentNotNullAndNotDeleted(providerSourceDatasetDocument))
                {
                    AddProviderSourceDatasetToResults(results, providerSourceDatasetDocument);
                }
            }

            return results;
        }

        private static void AddProviderSourceDatasetToResults(Dictionary<string, Dictionary<string, ProviderSourceDataset>> results, DocumentEntity<ProviderSourceDataset> providerSourceDatasetDocument)
        {
            ProviderSourceDataset providerSourceDatasetResult = providerSourceDatasetDocument.Content;

            string providerId = providerSourceDatasetDocument.Content.ProviderId;
            string dataRelationshipId = providerSourceDatasetDocument.Content.DataRelationship.Id;

            results[providerId].Add(dataRelationshipId, providerSourceDatasetResult);
        }

        private static bool IsReturnedDocumentNotNullAndNotDeleted(DocumentEntity<ProviderSourceDataset> providerSourceDatasetDocument)
        {
            return providerSourceDatasetDocument?.Deleted == false;
        }

        private void GenerateLookupRequestForDatasetForAProvider(string specificationId, IEnumerable<string> dataRelationshipIds, List<Task<DocumentEntity<ProviderSourceDataset>>> requests, string providerId)
        {
            foreach (string dataRelationshipId in dataRelationshipIds)
            {
                string documentKey = $"{specificationId}_{dataRelationshipId}_{providerId}";

                requests.Add(
                    _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(
                        () => _cosmosRepository.TryReadDocumentByIdPartitionedAsync<ProviderSourceDataset>(documentKey, providerId)));
            }
        }

        private static void EnsureResultsDictionaryContainsProvider(Dictionary<string, Dictionary<string, ProviderSourceDataset>> results, string providerId)
        {
            results.Add(providerId, new Dictionary<string, ProviderSourceDataset>());
        }
    }
}