using CalculateFunding.Models.Specifications.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specifications.Interfaces
{
    public interface IFundingStructureService
    {
        Task<IActionResult> GetFundingStructure(string fundingStreamId,
            string fundingPeriodId,
            string specificationId);

        Task<DateTimeOffset> GetFundingStructureTimeStamp(string fundingStreamId,
            string fundingPeriodId,
            string specificationId);

        Task<IActionResult> UpdateFundingStructureLastModified(UpdateFundingStructureLastModifiedRequest fundingStreamId);
    }
}
