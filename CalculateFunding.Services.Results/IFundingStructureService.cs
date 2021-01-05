using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Results
{
    [Obsolete]
    public interface IFundingStructureService
    {
        Task<DateTimeOffset> GetFundingStructureTimeStamp(string fundingStreamId, string fundingPeriodId, string specificationId);
        Task<IActionResult> GetFundingStructureWithCalculationResults(string fundingStreamId, string fundingPeriodId, string specificationId, string providerId = null);
    }
}