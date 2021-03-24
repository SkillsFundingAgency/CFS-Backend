using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GenerateApprovePublishedFundingCsvJobsCreation : BaseGeneratePublishedFundingCsvJobsCreation
    {
        public GenerateApprovePublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob)
            : base(createGeneratePublishedFundingCsvJobs, createGeneratePublishedProviderEstateCsvJob)
        {
        }

        public override async Task<IEnumerable<Job>> CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            Guard.IsNullOrWhiteSpace(publishedFundingCsvJobsRequest.SpecificationId, nameof(publishedFundingCsvJobsRequest.SpecificationId));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.User, nameof(publishedFundingCsvJobsRequest.User));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.FundingLineCodes, nameof(publishedFundingCsvJobsRequest.FundingLineCodes));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.FundingStreamIds, nameof(publishedFundingCsvJobsRequest.FundingStreamIds));

            return await CreatePublishedFundingCsvJobs(publishedFundingCsvJobsRequest);
        }

        public override bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return action == GeneratePublishingCsvJobsCreationAction.Approve;
        }
    }
}
