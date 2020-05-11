using CalculateFunding.Common.Models.HealthCheck;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingSchemaService
    {
        Task<IActionResult> SaveFundingSchema(string actionName, string controllerName, string schema);

        Task<IActionResult> GetFundingSchemaByVersion(string version);
    }
}
