using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IQueueReIndexSpecificationCalculationRelationships
    {
        Task<IActionResult> QueueForSpecification(string specificationId);
    }
}