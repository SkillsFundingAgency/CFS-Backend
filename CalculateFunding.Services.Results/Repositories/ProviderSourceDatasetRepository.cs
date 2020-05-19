using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results.Repositories
{
    public class ProviderSourceDatasetRepository : IProviderSourceDatasetRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public ProviderSourceDatasetRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderSourceDatasetRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT *
                            FROM    Root r
                            WHERE   r.content.providerId = @ProviderId
                                    AND r.content.specificationId = @SpecificationId
                                    AND r.documentType = @DocumentType 
                                    AND r.deleted = false",
                Parameters = new []
                {
                      new CosmosDbQueryParameter("@ProviderId", providerId),
                      new CosmosDbQueryParameter("@SpecificationId", specificationId), 
                      new CosmosDbQueryParameter("@DocumentType", nameof(ProviderSourceDataset))
                }
            };

            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(cosmosDbQuery, partitionKey: providerId);
        }

        public async Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT    r.content.providerId
                            FROM        Root r 
                            WHERE       r.content.specificationId = @SpecificationId
                                        AND r.documentType = @DocumentType
                                        AND r.deleted = false 
                                        AND r.content.definesScope = true",
                Parameters = new []
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId),
                    new CosmosDbQueryParameter("@DocumentType", nameof(ProviderSourceDataset))
                }
            };

            IEnumerable<dynamic> providerSourceDatasets = await _cosmosRepository.DynamicQuery(cosmosDbQuery);

            IEnumerable<string> providerIds = providerSourceDatasets.Select(m => (string)m.providerId).Distinct();

            return providerIds;
        }

        public async Task DeleteProviderSourceDataset(string providerSourceDatasetId, DeletionType deletionType)
        {
            IEnumerable<ProviderSourceDataset> providerSourceDatasets = 
                await _cosmosRepository.Query<ProviderSourceDataset>(m => m.Id == providerSourceDatasetId);

            List<ProviderSourceDataset> providerSourceDatasetList = providerSourceDatasets.ToList();

            if (!providerSourceDatasetList.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(providerSourceDatasetList.ToDictionary(c => c.ProviderId), hardDelete:false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(providerSourceDatasetList.ToDictionary(c => c.ProviderId), hardDelete:true);
        }

        public async Task DeleteProviderSourceDatasetVersion(string providerSourceDatasetId, DeletionType deletionType)
        {
            IEnumerable<ProviderSourceDatasetVersion> providerSourceDatasets = 
                await _cosmosRepository.Query<ProviderSourceDatasetVersion>(m => m.Id == providerSourceDatasetId);

            List<ProviderSourceDatasetVersion> providerSourceDatasetList = providerSourceDatasets.ToList();

            if (!providerSourceDatasetList.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(providerSourceDatasetList.ToDictionary(c => c.ProviderId), hardDelete:false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(providerSourceDatasetList.ToDictionary(c => c.ProviderId), hardDelete:true);
        }
    }
}
