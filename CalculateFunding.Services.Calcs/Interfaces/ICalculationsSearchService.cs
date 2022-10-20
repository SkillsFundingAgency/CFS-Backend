using System.Threading.Tasks;
using CalculateFunding.Models.Versioning;
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
            PublishStatus? status,
            string searchTerm,
            int? page);

        Task RemoveCalculations(string specificationId, string calcType);
    }
}
