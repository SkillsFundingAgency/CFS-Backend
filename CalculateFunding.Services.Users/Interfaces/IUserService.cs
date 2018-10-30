using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> GetUserByUserId(HttpRequest request);

        Task<IActionResult> ConfirmSkills(HttpRequest request);
    }
}
