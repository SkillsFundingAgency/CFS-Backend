using CalculateFunding.Models.Result.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IFundingStructureService
    {
        Task<IActionResult> GetFundingStructure(string fundingStreamId,
            string fundingPeriodId,
            string specificationId);

        Task<IActionResult> GetFundingStructureWithCalculationResults(string fundingStreamId,
            string fundingPeriodId,
            string specificationId,
            string providerId = null);

        Task<DateTimeOffset> GetFundingStructureTimeStamp(string fundingStreamId,
            string fundingPeriodId,
            string specificationId);

        Task<IActionResult> UpdateFundingStructureLastModified(UpdateFundingStructureLastModifiedRequest fundingStreamId);
    }
}
