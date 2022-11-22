using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public abstract class BaseGeneratePublishedFundingCsvJobsCreation : IGeneratePublishedFundingCsvJobsCreation
    {
        private readonly ICreatePublishingReportsJob _createPublishingReportsJob;
        private readonly ICreateGeneratePublishedFundingCsvJobs _createGeneratePublishedFundingCsvJobs;
        private readonly ICreateGeneratePublishedProviderEstateCsvJobs _createGeneratePublishedProviderEstateCsvJobs;
        private readonly ICreateGeneratePublishedProviderStateSummaryCsvJobs _createGeneratePublishedProviderStateSummaryCsvJob;
        private readonly ICreateGenerateChannelLevelPublishedGroupCsvJobs _createGenerateChannelLevelPublishedGroupCsvJob;

        protected BaseGeneratePublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob,
            ICreateGeneratePublishedProviderStateSummaryCsvJobs createGeneratePublishedProviderStateSummaryCsvJob,
            ICreatePublishingReportsJob createPublishingReportsJob,
            ICreateGenerateChannelLevelPublishedGroupCsvJobs createGenerateChannelLevelPublishedGroupCsvJob)
        {
            Guard.ArgumentNotNull(createGeneratePublishedFundingCsvJobs, nameof(createGeneratePublishedFundingCsvJobs));
            Guard.ArgumentNotNull(createGeneratePublishedProviderEstateCsvJob, nameof(createGeneratePublishedProviderEstateCsvJob));
            Guard.ArgumentNotNull(createGeneratePublishedProviderStateSummaryCsvJob, nameof(createGeneratePublishedProviderStateSummaryCsvJob));
            Guard.ArgumentNotNull(createGenerateChannelLevelPublishedGroupCsvJob, nameof(createGenerateChannelLevelPublishedGroupCsvJob));

            _createGeneratePublishedFundingCsvJobs = createGeneratePublishedFundingCsvJobs;
            _createGeneratePublishedProviderEstateCsvJobs = createGeneratePublishedProviderEstateCsvJob;
            _createGeneratePublishedProviderStateSummaryCsvJob = createGeneratePublishedProviderStateSummaryCsvJob;
            _createPublishingReportsJob = createPublishingReportsJob;
            _createGenerateChannelLevelPublishedGroupCsvJob = createGenerateChannelLevelPublishedGroupCsvJob;
        }

        public abstract Task<IEnumerable<Job>> CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest);

        public async Task<Job> CreatePublishingReportJob(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            return await _createPublishingReportsJob.CreateJob(
                publishedFundingCsvJobsRequest.SpecificationId,
                publishedFundingCsvJobsRequest.User,
                publishedFundingCsvJobsRequest.CorrelationId        
            );
        }

        protected async Task<IEnumerable<Job>> CreatePublishedProviderEstateCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();

            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                tasks.Add(_createGeneratePublishedProviderEstateCsvJobs.CreateJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.User,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    JobProperties(FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate, null, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId),
                    parentJobId: publishedFundingCsvJobsRequest.ParentJobId));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }

        protected async Task<IEnumerable<Job>> CreatePublishedOrganisationGroupCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();
            
            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                tasks.Add(CreatePublishedFundingCsvJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId, 
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues,
                    publishedFundingCsvJobsRequest.ParentJobId,
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));

                tasks.Add(CreatePublishedFundingCsvJob(publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues,
                    publishedFundingCsvJobsRequest.ParentJobId,
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }

        protected async Task<IEnumerable<Job>> CreatePublishedFundingCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();
            
            foreach (FundingLineCsvGeneratorJobType jobType in new[] { FundingLineCsvGeneratorJobType.CurrentState, FundingLineCsvGeneratorJobType.Released, FundingLineCsvGeneratorJobType.History })
            {
                tasks.Add(CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   jobType,
                   publishedFundingCsvJobsRequest.ParentJobId,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId,
                   fundingStreamId: publishedFundingCsvJobsRequest.FundingStreamIds.FirstOrDefault()));
            }
           
            foreach ((string Code, string Name) in publishedFundingCsvJobsRequest.FundingLines)
            {
                tasks.Add(CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.CurrentProfileValues,
                   publishedFundingCsvJobsRequest.ParentJobId,
                   fundingLineName: Name,
                   fundingLineCode: Code,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId,
                   fundingStreamId: publishedFundingCsvJobsRequest.FundingStreamIds.FirstOrDefault()));

                tasks.Add(CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.HistoryProfileValues,
                   publishedFundingCsvJobsRequest.ParentJobId,
                   fundingLineName: Name,
                   fundingLineCode: Code,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId,
                   fundingStreamId: publishedFundingCsvJobsRequest.FundingStreamIds.FirstOrDefault()));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }

        protected async Task<IEnumerable<Job>> CreatePublishedGroupsCsvJob(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();
            
            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                tasks.Add(CreatePublishedFundingCsvJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.PublishedGroups,
                    publishedFundingCsvJobsRequest.ParentJobId,
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }

        protected async Task<IEnumerable<Job>> CreateProviderCurrentStateSummaryCsvJob(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();

            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                tasks.Add(_createGeneratePublishedProviderStateSummaryCsvJob.CreateJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.User,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    JobProperties(FundingLineCsvGeneratorJobType.PublishedProviderStateSummary, null, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId),
                    parentJobId: publishedFundingCsvJobsRequest.ParentJobId));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }

        protected async Task<IEnumerable<Job>> CreateChannelLevelPublishedGroupCsvJob(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();

            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                tasks.Add(_createGenerateChannelLevelPublishedGroupCsvJob.CreateJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.User,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    JobProperties(FundingLineCsvGeneratorJobType.ChannelLevelPublishedGroup, null, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId),
                    parentJobId: publishedFundingCsvJobsRequest.ParentJobId));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }

        private Task<Job> CreatePublishedFundingCsvJob(
           string specificationId,
           string correlationId,
           Reference user,
           FundingLineCsvGeneratorJobType jobType,
           string parentJobId,
           string fundingLineName = null,
           string fundingStreamId = null,
           string fundingPeriodId = null,
           string fundingLineCode = null)
        {
            return _createGeneratePublishedFundingCsvJobs.CreateJob(specificationId,
                user, correlationId,
                JobProperties(jobType, fundingLineCode, fundingLineName, fundingStreamId, fundingPeriodId),
                parentJobId: parentJobId);
        }

        private Dictionary<string, string> JobProperties(FundingLineCsvGeneratorJobType jobType, 
            string fundingLineCode,
            string fundingLineName,
            string fundingStreamId, 
            string fundingPeriodId)
        {
            return new Dictionary<string, string>
            {
                {"job-type", jobType.ToString()},
                {"funding-line-code", fundingLineCode},
                {"funding-line-name", fundingLineName},
                {"funding-stream-id", fundingStreamId},
                {"funding-period-id", fundingPeriodId},
            };
        }

        public abstract bool IsForAction(GeneratePublishingCsvJobsCreationAction action);
    }
}