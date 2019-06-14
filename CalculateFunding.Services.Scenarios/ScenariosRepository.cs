using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Scenarios.Interfaces;

namespace CalculateFunding.Services.Scenarios
{
    public class ScenariosRepository : IScenariosRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public ScenariosRepository(CosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ScenariosRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
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
            {
                return null;
            }

            CurrentTestScenario currentTestScenario = new CurrentTestScenario
            {
                LastUpdatedDate = scenario.UpdatedAt,
                Id = scenario.Id,
                Name = scenario.Content.Name,
                Description = scenario.Content.Current.Description,
                Author = scenario.Content.Current.Author,
                Comment = scenario.Content.Current.Comment,
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
