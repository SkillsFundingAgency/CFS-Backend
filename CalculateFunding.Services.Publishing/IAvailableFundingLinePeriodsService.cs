using CalculateFunding.Models.Publishing;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public interface IAvailableFundingLinePeriodsService
    {
        Task<ActionResult<IEnumerable<AvailableVariationPointerFundingLine>>> GetAvailableFundingLineProfilePeriodsForVariationPointers(string specificationId);
    }
}