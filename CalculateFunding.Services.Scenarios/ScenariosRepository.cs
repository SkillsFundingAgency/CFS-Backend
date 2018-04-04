using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Scenarios.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosRepository : IScenariosRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ScenariosRepository(CosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            _cosmosRepository = cosmosRepository;
        }

        public Task<TestScenario> GetTestScenarioById(string testScenarioId)
        {
            var scenarios =  _cosmosRepository.Query<TestScenario>().Where(m => m.Id == testScenarioId);

            return Task.FromResult(scenarios.AsEnumerable().FirstOrDefault());
        }

        public Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            var scenarios = _cosmosRepository.Query<TestScenario>().Where(m => m.Specification.Id == specificationId);

            return Task.FromResult(scenarios.AsEnumerable());
        }

        public Task<HttpStatusCode> SaveTestScenario(TestScenario testScenario)
        {
            return _cosmosRepository.CreateAsync(testScenario);
        }
    }
}
