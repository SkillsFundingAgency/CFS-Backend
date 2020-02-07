using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProviderFundingPublishingService : IHealthChecker
    {
        Task<IActionResult> PublishProviderFunding(string specificationId,
            Reference user,
            string correlationId);

        Task<IActionResult> GetPublishedProviderTransactions(string specificationId,
            string providerId);

        Task<IActionResult> GetPublishedProviderVersion(string fundingStreamId,
                string fundingPeriodId,
                string providerId,
                string version);

    }
}