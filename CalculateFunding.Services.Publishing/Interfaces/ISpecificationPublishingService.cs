﻿using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationPublishingService
    {
        Task<IActionResult> ValidateSpecificationForRefresh(string specificationId);

        Task<IActionResult> CreateRefreshFundingJob(string specificationId,
            Reference user,
            string correlationId);

        Task<IActionResult> ApproveAllProviderFunding(string specificationId,
            Reference user,
            string correlationId);

        Task<IActionResult> CanChooseForFunding(string specificationId);

        Task<IActionResult> ApproveBatchProviderFunding(string specificationId,
            PublishedProviderIdsRequest approveProvidersRequest, 
            Reference user, 
            string correlationId);
    }
}