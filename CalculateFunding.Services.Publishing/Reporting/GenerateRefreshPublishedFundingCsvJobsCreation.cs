using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Helpers;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GenerateRefreshPublishedFundingCsvJobsCreation : BaseGeneratePublishedFundingCsvJobsCreation
    {
        public GenerateRefreshPublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob,
            ICreateGeneratePublishedProviderStateSummaryCsvJobs createGeneratePublishedProviderStateSummaryCsvJob,
            ICreatePublishingReportsJob createPublishingReportsJob,
            ICreateGenerateChannelLevelPublishedGroupCsvJobs createGenerateChannelLevelPublishedGroupCsvJob)
            : base(createGeneratePublishedFundingCsvJobs, 
                  createGeneratePublishedProviderEstateCsvJob, 
                  createGeneratePublishedProviderStateSummaryCsvJob,
                  createPublishingReportsJob,
                  createGenerateChannelLevelPublishedGroupCsvJob)
        {
        }

        public override async Task<IEnumerable<Job>> CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            Guard.IsNullOrWhiteSpace(publishedFundingCsvJobsRequest.SpecificationId, nameof(publishedFundingCsvJobsRequest.SpecificationId));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.User, nameof(publishedFundingCsvJobsRequest.User));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.FundingLines, nameof(publishedFundingCsvJobsRequest.FundingLines));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.FundingStreamIds, nameof(publishedFundingCsvJobsRequest.FundingStreamIds));

            List<Task<IEnumerable<Job>>> tasks = new List<Task<IEnumerable<Job>>>();

            tasks.Add(CreatePublishedFundingCsvJobs(publishedFundingCsvJobsRequest));
            tasks.Add(CreatePublishedProviderEstateCsvJobs(publishedFundingCsvJobsRequest));
            tasks.Add(CreateProviderCurrentStateSummaryCsvJob(publishedFundingCsvJobsRequest));
            //Todo Disablinging the  Channel level release functionality for the produciton release
            //tasks.Add(CreateChannelLevelPublishedGroupCsvJob(publishedFundingCsvJobsRequest));

            IEnumerable<Job>[] jobs = await TaskHelper.WhenAllAndThrow(tasks.ToArray());

            return jobs.SelectMany(_ => _);
        }

        public override bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return action == GeneratePublishingCsvJobsCreationAction.Refresh;
        }
    }
}
