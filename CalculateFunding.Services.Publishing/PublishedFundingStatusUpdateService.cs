using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingStatusUpdateService : IPublishedFundingStatusUpdateService
    {
        public Task UpdatePublishedFundingStatus(IEnumerable<PublishedFundingVersion> publishedFundingToSave, Reference author, PublishedProviderStatus released)
        {
            // Use existing versioning repositories etc to 
            // Increment major version of PublishedFundingVersion.

            // Ensure the version is stored in Cosmos

            // Set this version as Current in the PublishedFunding
            throw new NotImplementedException();
        }
    }
}
