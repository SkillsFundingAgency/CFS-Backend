using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GenerateReleasePublishedFundingCsvJobsCreation : BaseGeneratePublishedFundingCsvJobsCreation
    {
        public GenerateReleasePublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob,
            ICreateGeneratePublishedProviderStateSummaryCsvJobs createGeneratePublishedProviderStateSummaryCsvJob,
            ICreatePublishingReportsJob createPublishingReportsJob)
            : base(createGeneratePublishedFundingCsvJobs, 
                  createGeneratePublishedProviderEstateCsvJob, 
                  createGeneratePublishedProviderStateSummaryCsvJob,
                  createPublishingReportsJob)
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
            tasks.Add(CreatePublishedOrganisationGroupCsvJobs(publishedFundingCsvJobsRequest));
            tasks.Add(CreatePublishedGroupsCsvJob(publishedFundingCsvJobsRequest));
            tasks.Add(CreateProviderCurrentStateSummaryCsvJob(publishedFundingCsvJobsRequest));

            IEnumerable<Job>[] jobs = await TaskHelper.WhenAllAndThrow(tasks.ToArray());

            return jobs.SelectMany(_ => _);
        }

        public override bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return action == GeneratePublishingCsvJobsCreationAction.Release;
        }
    }
}
