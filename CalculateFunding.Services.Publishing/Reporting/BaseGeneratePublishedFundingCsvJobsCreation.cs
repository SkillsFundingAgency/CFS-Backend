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

        public abstract Task CreateJobs(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingLineCodes);

        protected async Task CreatePublishedProviderEstateCsvJobs(string specificationId, string correlationId, Reference user)
        {
            await _createGeneratePublishedProviderEstateCsvJobs.CreateJob(specificationId, user, correlationId);
        }

        protected async Task CreatePublishedFundingCsvJobs(string specificationId, string correlationId, Reference user, IEnumerable<string> fundingLineCodes)
        {
            await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentState);
            await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.Released);
            await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.History);

            foreach (string fundingLineCode in fundingLineCodes)
            {
                await CreatePublishedFundingCsvJob(specificationId, correlationId, user, FundingLineCsvGeneratorJobType.CurrentProfileValues, fundingLineCode);
            }
        }

        private Task<Job> CreatePublishedFundingCsvJob(string specification, string correlationId, Reference user, FundingLineCsvGeneratorJobType jobType, string fundingLineCode = null)
        {
            return _createGeneratePublishedFundingCsvJobs.CreateJob(specification, user, correlationId, JobProperties(jobType, fundingLineCode));
        }

        private Dictionary<string, string> JobProperties(FundingLineCsvGeneratorJobType jobType, string fundingLineCode = null)
        {
            return new Dictionary<string, string>
            {
                {"job-type", jobType.ToString()},
                {"funding-line-code", fundingLineCode}
            };
        }

        public abstract bool IsForAction(GeneratePublishingCsvJobsCreationAction action);
    }
}