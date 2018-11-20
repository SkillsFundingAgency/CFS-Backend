using CalculateFunding.Models.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobDefinitionsService
    {
        Task<IActionResult> SaveDefinition(HttpRequest request);

        Task<IActionResult> GetJobDefinitions();

        Task<IEnumerable<JobDefinition>> GetAllJobDefinitions();

        Task<IActionResult> GetJobDefinitionById(string jobDefinitionId);
    }
}
