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

        public abstract Task CreateJobs(string specificationId, 
            string correlationId, 
            Reference user, 
            IEnumerable<string> fundingLineCodes = null, 
            IEnumerable<string> fundingStreamIds = null,
            string fundingPeriodId = null);

        protected async Task CreatePublishedProviderEstateCsvJobs(string specificationId, string correlationId, Reference user)
        {
            await _createGeneratePublishedProviderEstateCsvJobs.CreateJob(specificationId, user, correlationId);
        }

        //TODO; we might want to replace this long parameter list with a single parameter object as changing it cascades across many duplicate method signatures
        protected async Task CreatePublishedOrganisationGroupCsvJobs(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingStreamIds, string fundingPeriod)
        {
            foreach (string fundingStreamId in fundingStreamIds)
            {
                await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues, fundingStreamId: fundingStreamId, fundingPeriodId: fundingPeriod);
                await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues, fundingStreamId: fundingStreamId, fundingPeriodId: fundingPeriod);
            }
        }

        protected async Task CreatePublishedFundingCsvJobs(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingLineCodes, string fundingPeriodId)
        {
            await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentState, fundingPeriodId: fundingPeriodId);
            await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.Released, fundingPeriodId: fundingPeriodId);
            await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.History, fundingPeriodId: fundingPeriodId);

            foreach (string fundingLineCode in fundingLineCodes)
            {
                await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentProfileValues, fundingLineCode, fundingPeriodId: fundingPeriodId);
                await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.HistoryProfileValues, fundingLineCode, fundingPeriodId: fundingPeriodId);
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