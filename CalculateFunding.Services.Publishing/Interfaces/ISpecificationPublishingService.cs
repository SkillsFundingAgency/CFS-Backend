using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationPublishingService
    {
        Task<IActionResult> CreatePublishJob(string specificationId,
            Reference user,
            string correlationId);
    }
}