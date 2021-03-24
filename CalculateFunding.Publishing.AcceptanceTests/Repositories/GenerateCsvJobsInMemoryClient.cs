using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using System;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class GenerateCsvJobsInMemoryClient : IGeneratePublishedFundingCsvJobsCreation
    {
        public async Task<IEnumerable<Job>> CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            RequestedSpecificationIds.Add(publishedFundingCsvJobsRequest.SpecificationId);
            
            return await Task.FromResult<IEnumerable<Job>>(Array.Empty<Job>());
        }

        public bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return true;
        }

        public ICollection<string> RequestedSpecificationIds { get; } = new List<string>();
    }
}