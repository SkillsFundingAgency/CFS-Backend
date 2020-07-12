using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
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

        public abstract Task CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest);

        protected async Task CreatePublishedProviderEstateCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                await _createGeneratePublishedProviderEstateCsvJobs.CreateJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.User,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    JobProperties(FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate, null, fundingStreamId, publishedFundingCsvJobsRequest.FundingPeriodId));
            }
        }

        protected async Task CreatePublishedOrganisationGroupCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                await CreatePublishedFundingCsvJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId, 
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, 
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId);

                await CreatePublishedFundingCsvJob(publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, 
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId);
            }
        }

        protected async Task CreatePublishedFundingCsvJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            foreach (FundingLineCsvGeneratorJobType jobType in new[] { FundingLineCsvGeneratorJobType.CurrentState, FundingLineCsvGeneratorJobType.Released, FundingLineCsvGeneratorJobType.History })
            {
                await CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   jobType,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId);
            }
           
            foreach (string fundingLineCode in publishedFundingCsvJobsRequest.FundingLineCodes)
            {
                await CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.CurrentProfileValues,
                   fundingLineCode,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId);

                await CreatePublishedFundingCsvJob(
                   publishedFundingCsvJobsRequest.SpecificationId,
                   publishedFundingCsvJobsRequest.CorrelationId,
                   publishedFundingCsvJobsRequest.User,
                   FundingLineCsvGeneratorJobType.HistoryProfileValues,
                   fundingLineCode,
                   fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId);
            }
        }

        protected async Task CreatePublishedGroupsCsvJob(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            foreach (string fundingStreamId in publishedFundingCsvJobsRequest.FundingStreamIds)
            {
                await CreatePublishedFundingCsvJob(
                    publishedFundingCsvJobsRequest.SpecificationId,
                    publishedFundingCsvJobsRequest.CorrelationId,
                    publishedFundingCsvJobsRequest.User, 
                    FundingLineCsvGeneratorJobType.PublishedGroups, 
                    fundingStreamId: fundingStreamId, 
                    fundingPeriodId: publishedFundingCsvJobsRequest.FundingPeriodId);
            }
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