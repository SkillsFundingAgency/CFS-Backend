using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Specs.Interfaces
{
    public interface ISpecificationIndexingService
    {
        Task<IActionResult> QueueSpecificationIndexJob(string specificationId,
            Reference user,
            string correlationId);

        Task Run(Message message);
    }
}