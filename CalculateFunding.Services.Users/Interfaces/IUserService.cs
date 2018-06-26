using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult> GetUserByUsername(HttpRequest request);

        Task<IActionResult> ConfirmSkills(HttpRequest request);
    }
}
