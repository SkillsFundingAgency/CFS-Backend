using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserIndexingService: IProcessingService
    {
        Task<IActionResult> ReIndex(Reference user, string correlationId);
    }
}
