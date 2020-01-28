using CalculateFunding.Models.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingPeriodService
    {
        Task<IActionResult> GetFundingPeriods();

        Task<IActionResult> GetFundingPeriodById(string fundingPeriodId);

        Task<IActionResult> SaveFundingPeriods(FundingPeriodsJsonModel fundingPeriodsJsonModel);
    }
}
