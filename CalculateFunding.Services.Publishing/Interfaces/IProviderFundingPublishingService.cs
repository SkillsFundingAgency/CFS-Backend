using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProviderFundingPublishingService
    {
        Task<IActionResult> PublishProviderFunding(string specificationId,
            Reference user,
            string correlationId);
    }
}