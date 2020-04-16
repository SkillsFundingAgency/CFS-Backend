using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderProfilingService
    {
        Task<IActionResult> AssignProfilePatternKey(
            string fundingStreamId, 
            string fundingPeriodId, 
            string providerId, 
            ProfilePatternKey profilePatternKey,
            Reference author);
    }
}
