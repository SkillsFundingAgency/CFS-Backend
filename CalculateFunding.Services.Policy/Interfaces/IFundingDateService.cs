using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingDateService
    {
        Task<IActionResult> GetFundingDate(
            string fundingStreamId, 
            string fundingPeriodId, 
            string fundingLineId);
        
        Task<IActionResult> SaveFundingDate(
            string actionName,
            string controllerName,
            string fundingStreamId, 
            string fundingPeriodId, 
            string fundingLineId,
            FundingDateViewModel fundingDateViewModel);
    }
}
