using System.Threading.Tasks;
using CalculateFunding.Models.Users;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> GetUserByUserId(string userId);

        Task<IActionResult> ConfirmSkills(string userId, UserCreateModel userCreateModel);
    }
}
