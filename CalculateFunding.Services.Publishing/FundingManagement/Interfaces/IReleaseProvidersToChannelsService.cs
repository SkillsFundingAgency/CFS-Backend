﻿using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IReleaseProvidersToChannelsService : IProcessingService
    {
        Task<IActionResult> QueueReleaseProviderVersions(string specificationId, ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest, Reference author, string correlationId);

        Task ReleaseProviderVersions(string specificationId, ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest, Reference author, string correlationId);
    }
}