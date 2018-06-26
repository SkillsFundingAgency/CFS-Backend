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

        public Task<IEnumerable<DocumentEntity<TestScenario>>> GetAllTestScenarios()
        {
            return Task.FromResult(_cosmosRepository.Read<TestScenario>().AsEnumerable());
        }

        public Task<TestScenario> GetTestScenarioById(string testScenarioId)
        {
            var scenarios = _cosmosRepository.Query<TestScenario>().Where(m => m.Id == testScenarioId);

            return Task.FromResult(scenarios.AsEnumerable().FirstOrDefault());
        }

        public async Task<CurrentTestScenario> GetCurrentTestScenarioById(string testScenarioId)
        {
            Guard.IsNullOrWhiteSpace(testScenarioId, nameof(testScenarioId));

            DocumentEntity<TestScenario> scenario = await _cosmosRepository.ReadAsync<TestScenario>(testScenarioId);

            if (scenario == null)
                return null;

            CurrentTestScenario currentTestScenario = new CurrentTestScenario
            {
                LastUpdatedDate = scenario.UpdatedAt,
                Id = scenario.Id,
                Name = scenario.Content.Name,
                Description = scenario.Content.Current.Description,
                Author = scenario.Content.Current.Author,
                Commment = scenario.Content.Current.Commment,
                CurrentVersionDate = scenario.Content.Current.Date,
                PublishStatus = scenario.Content.Current.PublishStatus,
                Gherkin = scenario.Content.Current.Gherkin,
                Version = scenario.Content.Current.Version,
                SpecificationId = scenario.Content.SpecificationId,
            };

            return currentTestScenario;
        }

        public Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            var scenarios = _cosmosRepository.Query<TestScenario>().Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(scenarios.AsEnumerable());
        }

        public Task<HttpStatusCode> SaveTestScenario(TestScenario testScenario)
        {
            return _cosmosRepository.UpsertAsync(testScenario);
        }

        public Task SaveTestScenarios(IEnumerable<TestScenario> testScenarios)
        {
            return _cosmosRepository.BulkCreateAsync<TestScenario>(testScenarios.ToList());
        }
    }
}
