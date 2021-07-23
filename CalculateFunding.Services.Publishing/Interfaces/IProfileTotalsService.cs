using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Profiling;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProfileTotalsService
    {
        Task<IActionResult> GetPaymentProfileTotalsForFundingStreamForProvider(
            string fundingStreamId, 
            string fundingPeriodId, 
            string providerId);

        Task<IActionResult> GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(
            string fundingStreamId, 
            string fundingPeriodId, 
            string providerId);

        Task<ActionResult<FundingLineProfile>> GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode);

        Task<IActionResult> PreviousProfileExistsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode);

        Task<IActionResult> GetPreviousProfilesForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode);

        Task<IActionResult> GetCurrentProfileConfig(
            string specificationId,
            string providerId,
            string fundingStreamId);
    }
}