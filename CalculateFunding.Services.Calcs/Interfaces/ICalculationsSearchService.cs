using System.Threading.Tasks;
using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationsSearchService
    {
        Task<IActionResult> SearchCalculations(SearchModel searchModel);
    }
}
