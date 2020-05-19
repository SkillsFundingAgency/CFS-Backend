using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetsAggregationsRepository : IDatasetsAggregationsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public DatasetsAggregationsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var (Ok, Message) = _cosmosRepository.IsHealthOk();

            health.Name = nameof(DatasetsAggregationsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = this.GetType().Name, Message = Message });

            return Task.FromResult(health);
        }

        public async Task CreateDatasetAggregations(DatasetAggregations datasetAggregations)
        {
            await _cosmosRepository.CreateAsync<DatasetAggregations>(datasetAggregations);
        }

        public async Task<IEnumerable<DatasetAggregations>> GetDatasetAggregationsForSpecificationId(string specificationId)
        {
            return (await _cosmosRepository.Query<DatasetAggregations>(x => x.Content.SpecificationId == specificationId));
        }
    }
}
