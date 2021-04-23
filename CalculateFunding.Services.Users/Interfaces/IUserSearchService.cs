using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserSearchService
    {
        Task<ActionResult> SearchUsers(SearchModel searchModel);
    }
}
