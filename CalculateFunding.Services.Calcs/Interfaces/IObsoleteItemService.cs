using CalculateFunding.Models.Calcs.ObsoleteItems;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IObsoleteItemService
    {
        Task<IActionResult> GetObsoleteItemsForSpecification(string specificationId);
        Task<IActionResult> GetObsoleteItemsForCalculation(string calculationId);
        Task<IActionResult> CreateObsoleteItem(ObsoleteItem obsoleteItem);
        Task<IActionResult> AddCalculationToObsoleteItem(string obsoleteItemId, string calculationId);
        Task<IActionResult> RemoveObsoleteItem(string obsoleteItemId, string calculationId);
    }
}
