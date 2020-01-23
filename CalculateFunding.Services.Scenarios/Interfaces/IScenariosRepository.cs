using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosRepository
    {
        Task<TestScenario> GetTestScenarioById(string testScenarioId);

        Task<HttpStatusCode> SaveTestScenario(TestScenario testScenario);

        Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId);

        Task<IEnumerable<DocumentEntity<TestScenario>>> GetAllTestScenarios();

        Task<CurrentTestScenario> GetCurrentTestScenarioById(string testScenarioId);

        Task SaveTestScenarios(IEnumerable<TestScenario> testScenarios);

        Task DeleteTestsBySpecificationId(string specificationId, DeletionType deletionType);
    }
}
