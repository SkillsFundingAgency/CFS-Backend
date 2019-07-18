using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationPublishingService
    {
        Task<IActionResult> CreatePublishJob(string specificationId,
            Reference user,
            string correlationId);

        Task<IActionResult> ApproveSpecification(string action,
            string controller,
            string specificationId,
            HttpRequest request,
            Reference user,
            string correlationId);
    }
}