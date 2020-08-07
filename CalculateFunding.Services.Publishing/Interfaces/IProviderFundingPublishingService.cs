using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProviderFundingPublishingService
    {
        Task<IActionResult> PublishAllProvidersFunding(string specificationId,
            Reference user,
            string correlationId);

        Task<IActionResult> PublishBatchProvidersFunding(string specificationId,
            PublishedProviderIdsRequest publishProvidersRequest,
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