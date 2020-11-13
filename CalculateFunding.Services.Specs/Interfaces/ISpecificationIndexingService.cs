using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationIndexingService : IJobProcessingService
    {
        Task<IActionResult> QueueSpecificationIndexJob(string specificationId,
            Reference user,
            string correlationId);
    }
}