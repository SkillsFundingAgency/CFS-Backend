using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IGeneratePublishedFundingCsvJobsCreation
    {
        Task CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest);
        
        bool IsForAction(GeneratePublishingCsvJobsCreationAction action);
    }
}