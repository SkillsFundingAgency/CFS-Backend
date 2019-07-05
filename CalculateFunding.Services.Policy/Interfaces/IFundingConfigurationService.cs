using CalculateFunding.Models.FundingPolicy.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingConfigurationService
    {
        Task<IActionResult> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId);

        Task<IActionResult> SaveFundingConfiguration(string actionName, string controllerName, FundingConfigurationViewModel configuration, string fundingStreamId, string fundingPeriodId);
    }
}
