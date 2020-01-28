using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobDefinitionsService
    {
        Task<IActionResult> SaveDefinition(string json, string jsonFilename);

        Task<IActionResult> GetJobDefinitions();

        Task<IEnumerable<JobDefinition>> GetAllJobDefinitions();

        Task<IActionResult> GetJobDefinitionById(string jobDefinitionId);
    }
}
