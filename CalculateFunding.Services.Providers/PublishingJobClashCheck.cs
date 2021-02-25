using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Providers.Interfaces;

namespace CalculateFunding.Services.Providers
{
    public class PublishingJobClashCheck : IPublishingJobClashCheck
    {
       private readonly IJobManagement _jobManagement;

       private static readonly string[] PublishingJobs = new[]
       {
            JobConstants.DefinitionNames.RefreshFundingJob,
            JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
            JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
            JobConstants.DefinitionNames.PublishAllProviderFundingJob,
            JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
       };

       public PublishingJobClashCheck(IJobManagement jobManagement)
       {
           Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
           
           _jobManagement = jobManagement;
       }

       public async Task<bool> PublishingJobsClashWithFundingStreamCoreProviderUpdate(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            
            IEnumerable<JobSummary> jobSummaries = await _jobManagement.GetLatestJobsForSpecification(specificationId, PublishingJobs);

            return jobSummaries?.Any(_ => _ != null && _.RunningStatus == RunningStatus.InProgress) == true;
        }
    }
}