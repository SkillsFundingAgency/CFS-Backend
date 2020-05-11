using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Scenarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosService
    {
        Task<IActionResult> SaveVersion(CreateNewTestScenarioVersion createNewTestScenarioVersion, Reference user, string correlationId);

        Task<IActionResult> GetTestScenariosBySpecificationId(string specificationId);

        Task<IActionResult> GetTestScenarioById(string scenarioId);

        Task<IActionResult> GetCurrentTestScenarioById(string scenarioId);

        Task UpdateScenarioForSpecification(Message message);

        Task UpdateScenarioForCalculation(Message message);

        Task ResetScenarioForFieldDefinitionChanges(IEnumerable<DatasetSpecificationRelationshipViewModel> relationships, string specificationId, IEnumerable<string> currentFieldDefinitionNames);

        Task<IActionResult> DeleteTests(Message message);
    }
}
