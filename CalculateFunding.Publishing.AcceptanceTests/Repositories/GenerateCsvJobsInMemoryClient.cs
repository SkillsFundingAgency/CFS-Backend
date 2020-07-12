using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class GenerateCsvJobsInMemoryClient : IGeneratePublishedFundingCsvJobsCreation
    {
        public Task CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            RequestedSpecificationIds.Add(publishedFundingCsvJobsRequest.SpecificationId);
            
            return Task.CompletedTask;
        }

        public bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return true;
        }

        public ICollection<string> RequestedSpecificationIds { get; } = new List<string>();
    }
}