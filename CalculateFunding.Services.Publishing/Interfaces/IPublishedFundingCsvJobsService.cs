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
        Task<(Job ParentJob, IEnumerable<Job> ChildJobs)> QueueCsvPublishingJobs(GeneratePublishingCsvJobsCreationAction createActionType, 
            string specificationId, 
            string correlationId, 
            Reference author,
            Job parentJob = null);
        Task<(Job ParentJob, IEnumerable<Job> ChildJobs)> QueueCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, 
            string specificationId, 
            string correlationId, 
            Reference author, 
            bool queueParentJob = false,
            Job parentJob = null);
        Task<(Job ParentJob, IEnumerable<Job> ChildJobs)> GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction createActionType, 
            string specificationId, 
            string fundingPeriodId, 
            IEnumerable<string> fundingStreamIds, 
            string correlationId, 
            Reference author, 
            bool queueParentJob = false,
            Job parentJob = null);
    }
}
