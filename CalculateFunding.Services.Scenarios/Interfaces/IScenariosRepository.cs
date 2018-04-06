using CalculateFunding.Models.Scenarios;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosRepository
    {
        Task<TestScenario> GetTestScenarioById(string testScenarioId);

        Task<HttpStatusCode> SaveTestScenario(TestScenario testScenario);

        Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId);
    }
}
