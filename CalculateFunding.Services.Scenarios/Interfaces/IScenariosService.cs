using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosService : IProcessingService
    {
        Task<IActionResult> SaveVersion(CreateNewTestScenarioVersion createNewTestScenarioVersion, Reference user, string correlationId);

        Task<IActionResult> GetTestScenariosBySpecificationId(string specificationId);

        Task<IActionResult> GetTestScenarioById(string scenarioId);

        Task<IActionResult> GetCurrentTestScenarioById(string scenarioId);

        Task ResetScenarioForFieldDefinitionChanges(IEnumerable<DatasetSpecificationRelationshipViewModel> relationships, string specificationId, IEnumerable<string> currentFieldDefinitionNames);

        Task DeleteTests(Message message);
    }
}
