using System.Threading.Tasks;
using CalculateFunding.Models.FundingPolicy.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingConfigurationService
    {
        Task<IActionResult> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId);

        Task<IActionResult> SaveFundingConfiguration(string actionName, string controllerName, FundingConfigurationViewModel configuration, string fundingStreamId, string fundingPeriodId);

        Task<IActionResult> GetFundingConfigurationsByFundingStreamId(string fundingStreamId);
    }
}
