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
        private readonly ICreateGeneratePublishedFundingCsvJobs _createGeneratePublishedFundingCsvJobs;
        private readonly ICreateGeneratePublishedProviderEstateCsvJobs _createGeneratePublishedProviderEstateCsvJobs;
        private readonly ICreateGeneratePublishedProviderStateSummaryCsvJobs _createGeneratePublishedProviderStateSummaryCsvJob;

        protected BaseGeneratePublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob,
            ICreateGeneratePublishedProviderStateSummaryCsvJobs createGeneratePublishedProviderStateSummaryCsvJob)
        {
            Guard.ArgumentNotNull(createGeneratePublishedFundingCsvJobs, nameof(createGeneratePublishedFundingCsvJobs));
            Guard.ArgumentNotNull(createGeneratePublishedProviderEstateCsvJob, nameof(createGeneratePublishedProviderEstateCsvJob));
            Guard.ArgumentNotNull(createGeneratePublishedProviderStateSummaryCsvJob, nameof(createGeneratePublishedProviderStateSummaryCsvJob));

            _createGeneratePublishedFundingCsvJobs = createGeneratePublishedFundingCsvJobs;
            _createGeneratePublishedProviderEstateCsvJobs = createGeneratePublishedProviderEstateCsvJob;
            _createGeneratePublishedProviderStateSummaryCsvJob = createGeneratePublishedProviderStateSummaryCsvJob;
        }

        public abstract Task<IEnumerable<Job>> CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest);

        protected async Task<IEnumerable<Job>> CreatePublishedProviderEstateCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            List<Task<Job>> tasks = new List<Task<Job>>();

            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                tasks.Add(_createGeneratePublishedProviderEstateCsvJobs.CreateJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.User,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    JobProperties(FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate, null, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId)));
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
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));

                tasks.Add(CreatePublishedFundingCsvJob(publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, 
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
                   fundingLineName: Name,
                   fundingLineCode: Code,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId,
                   fundingStreamId: publishedFundingCsvJobsRequest.FundingStreamIds.FirstOrDefault()));

                tasks.Add(CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.HistoryProfileValues,
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
                    JobProperties(FundingLineCsvGeneratorJobType.PublishedProviderStateSummary, null, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId)));
            }

            return await TaskHelper.WhenAllAndThrow(tasks.ToArray());
        }
        
        private Task<Job> CreatePublishedFundingCsvJob(
           string specificationId,
           string correlationId,
           Reference user,
           FundingLineCsvGeneratorJobType jobType,
           string fundingLineName = null,
           string fundingStreamId = null,
           string fundingPeriodId = null,
           string fundingLineCode = null)
        {
            return _createGeneratePublishedFundingCsvJobs.CreateJob(specificationId, user, correlationId, JobProperties(jobType, fundingLineCode, fundingLineName, fundingStreamId, fundingPeriodId));
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