using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishPrerequisiteChecker : IPublishPrerequisiteChecker
    {
        public Task<IEnumerable<string>> PerformPrerequisiteChecks(SpecificationSummary specification, IEnumerable<PublishedProvider> publishedProviders)
        {
            // Ensure this specification is already chosen for funding (use existing service)

            // Ensure all PublishedProviders have the status of Approved
            throw new NotImplementedException();
        }
    }
}
