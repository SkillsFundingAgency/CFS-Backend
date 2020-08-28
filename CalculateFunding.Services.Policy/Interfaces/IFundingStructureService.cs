using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Policy.Interfaces
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