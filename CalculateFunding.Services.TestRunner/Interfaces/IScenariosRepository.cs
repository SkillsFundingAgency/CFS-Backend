using CalculateFunding.Models.Scenarios;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{

    public interface IScenariosRepository
    {
        Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId);
    }
}
