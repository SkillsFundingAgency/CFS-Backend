using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICustomProfileService
    {
        Task<IActionResult> ApplyCustomProfile(ApplyCustomProfileRequest request, Reference author);
    }
}