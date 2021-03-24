using System.Collections.Generic;
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

        protected BaseGeneratePublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob)
        {
            Guard.ArgumentNotNull(createGeneratePublishedFundingCsvJobs, nameof(createGeneratePublishedFundingCsvJobs));
            Guard.ArgumentNotNull(createGeneratePublishedProviderEstateCsvJob, nameof(createGeneratePublishedProviderEstateCsvJob));

            _createGeneratePublishedFundingCsvJobs = createGeneratePublishedFundingCsvJobs;
            _createGeneratePublishedProviderEstateCsvJobs = createGeneratePublishedProviderEstateCsvJob;
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
                    JobProperties(FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId)));
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
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));
            }
           
            foreach (string fundingLineCode in publishedFundingCsvJobsRequest.FundingLineCodes)
            {
                tasks.Add(CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.CurrentProfileValues,
                   fundingLineCode,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));

                tasks.Add(CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.HistoryProfileValues,
                   fundingLineCode,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId));
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
        
        private Task<Job> CreatePublishedFundingCsvJob(
           string specificationId,
           string correlationId,
           Reference user,
           FundingLineCsvGeneratorJobType jobType,
           string fundingLineCode = null,
           string fundingStreamId = null,
           string fundingPeriodId = null)
        {
            return _createGeneratePublishedFundingCsvJobs.CreateJob(specificationId, user, correlationId, JobProperties(jobType, fundingLineCode, fundingStreamId, fundingPeriodId));
        }

        private Dictionary<string, string> JobProperties(FundingLineCsvGeneratorJobType jobType, 
            string fundingLineCode, 
            string fundingStreamId, 
            string fundingPeriodId)
        {
            return new Dictionary<string, string>
            {
                {"job-type", jobType.ToString()},
                {"funding-line-code", fundingLineCode},
                {"funding-stream-id", fundingStreamId},
                {"funding-period-id", fundingPeriodId},
            };
        }

        public abstract bool IsForAction(GeneratePublishingCsvJobsCreationAction action);
    }
}