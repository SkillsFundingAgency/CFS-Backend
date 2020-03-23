using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationsSearchService
    {
        Task<IActionResult> SearchCalculations(SearchModel searchModel);

        Task<IActionResult> SearchCalculations(string specificationId, 
            CalculationType calculationType, 
            string searchTerm,
            int? page);
    }
}
