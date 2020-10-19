using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Profiling.Services
{
    public interface IReprofilingService
    {
        Task<ActionResult<ReProfileResponse>> ReProfile(ReProfileRequest reProfileRequest);
    }
}