﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingStatusUpdateService
    {
        Task UpdatePublishedFundingStatus(IEnumerable<PublishedFundingVersion> publishedFundingToSave, Reference author, PublishedProviderStatus released);
    }
}