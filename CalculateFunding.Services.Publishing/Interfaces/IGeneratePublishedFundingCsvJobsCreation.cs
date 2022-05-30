using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IGeneratePublishedFundingCsvJobsCreation
    {
        Task<IEnumerable<Job>> CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest);

        Task<Job> CreatePublishingReportJob(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest);

        bool IsForAction(GeneratePublishingCsvJobsCreationAction action);
    }
}