using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class GenerateCsvJobsInMemoryClient : IGeneratePublishedFundingCsvJobsCreation
    {
        public Task CreateJobs(string specificationId, string correlationId, Reference user)
        {
            RequestedSpecificationIds.Add(specificationId);
            
            return Task.CompletedTask;
        }
        
        public ICollection<string> RequestedSpecificationIds { get; } = new List<string>();
    }
}