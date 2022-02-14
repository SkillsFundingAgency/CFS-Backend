using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class ReleaseManagementBlobStepContext : IReleaseManagementBlobStepContext
    {
        public InMemoryBlobClient FundingGroupsClient { get; set; }

        public InMemoryAzureBlobClient PublishedProvidersClient { get; set; }

        public InMemoryBlobClient PublishedFundingClient { get; set; }

        public InMemoryBlobClient ReleasedProvidersClient { get; set; }
    }
}
