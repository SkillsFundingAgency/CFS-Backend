using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IReferencedSpecificationReMapService : IJobProcessingService
    {
        Task<IActionResult> QueueReferencedSpecificationReMapJob(string specificationId,
            string datsetRelationshipId,
            Reference user,
            string correlationId);
    }
}
