using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserIndexingService : IProcessingService
    {
        Task<IActionResult> ReIndex(Reference invokedUser, string correlationId);
        Task IndexUsers(IEnumerable<User> users);
    }
}
