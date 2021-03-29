using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingCsvJobsService
    {
        Task<IEnumerable<Job>> QueueCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, string specificationId, string correlationId, Reference author);
        Task<IEnumerable<Job>> GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, string specificationId, string fundingPeriodId, IEnumerable<string> fundingStreamIds, string correlationId, Reference author);
    }
}
