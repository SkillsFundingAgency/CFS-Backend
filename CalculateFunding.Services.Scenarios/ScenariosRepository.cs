using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Messages;
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
            var cosmosRepoHealth = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ScenariosRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public async Task<IEnumerable<DocumentEntity<TestScenario>>> GetAllTestScenarios()
        {
            return await _cosmosRepository.Read<TestScenario>();
        }

        public async Task<TestScenario> GetTestScenarioById(string testScenarioId)
        {
            var scenarios = (await _cosmosRepository.Query<TestScenario>(m => m.Id == testScenarioId));

            return scenarios.FirstOrDefault();
        }

        public async Task<CurrentTestScenario> GetCurrentTestScenarioById(string testScenarioId)
        {
            Guard.IsNullOrWhiteSpace(testScenarioId, nameof(testScenarioId));

            DocumentEntity<TestScenario> scenario = await _cosmosRepository.ReadDocumentByIdAsync<TestScenario>(testScenarioId);

            if (scenario?.Content == null) return null;

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

        public async Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            return (await _cosmosRepository.Query<TestScenario>(m => m.Content.SpecificationId == specificationId));
        }

        public Task<HttpStatusCode> SaveTestScenario(TestScenario testScenario)
        {
            return _cosmosRepository.UpsertAsync(testScenario);
        }

        public Task SaveTestScenarios(IEnumerable<TestScenario> testScenarios)
        {
            return _cosmosRepository.BulkCreateAsync<TestScenario>(testScenarios.ToList());
        }

        public async Task DeleteTestsBySpecificationId(string specificationId, DeletionType deletionType)
        {
            IEnumerable<TestScenario> testResults = await GetTestsBySpecificationId(specificationId);

            IEnumerable<TestScenario> tests = await GetTestsBySpecificationId(specificationId);

            List<TestScenario> allTests = tests.ToList();

            if (!allTests.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(allTests.ToDictionary(c => c.Id), hardDelete: false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(allTests.ToDictionary(c => c.Id), hardDelete: true);
        }

        private async Task<IEnumerable<TestScenario>> GetTestsBySpecificationId(string specificationId)
        {
            return await _cosmosRepository.Query<TestScenario>(x => x.Content.SpecificationId == specificationId);
        }
    }
}
