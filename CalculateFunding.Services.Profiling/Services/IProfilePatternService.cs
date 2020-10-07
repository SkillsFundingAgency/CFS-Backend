using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Profiling.Services
{
    public interface IProfilePatternService
    {
        Task<IActionResult> CreateProfilePattern(CreateProfilePatternRequest createProfilePatternRequest);
        
        Task<IActionResult> UpsertProfilePattern(UpsertProfilePatternRequest upsertProfilePatternRequest);
        
        Task<IActionResult> DeleteProfilePattern(string id);
        Task<IActionResult> GetProfilePattern(string id);

        Task<IActionResult> GetProfilePatterns(string fundingStreamId,
            string fundingPeriodId);
    }
}