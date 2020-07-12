using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class GenerateReleasePublishedFundingCsvJobsCreation : BaseGeneratePublishedFundingCsvJobsCreation
    {
        public GenerateReleasePublishedFundingCsvJobsCreation(
            ICreateGeneratePublishedFundingCsvJobs createGeneratePublishedFundingCsvJobs,
            ICreateGeneratePublishedProviderEstateCsvJobs createGeneratePublishedProviderEstateCsvJob)
            : base(createGeneratePublishedFundingCsvJobs, createGeneratePublishedProviderEstateCsvJob)
        {
        }

        public override async Task CreateJobs(PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest)
        {
            Guard.IsNullOrWhiteSpace(publishedFundingCsvJobsRequest.SpecificationId, nameof(publishedFundingCsvJobsRequest.SpecificationId));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.User, nameof(publishedFundingCsvJobsRequest.User));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.FundingLineCodes, nameof(publishedFundingCsvJobsRequest.FundingLineCodes));
            Guard.ArgumentNotNull(publishedFundingCsvJobsRequest.FundingStreamIds, nameof(publishedFundingCsvJobsRequest.FundingStreamIds));

            await CreatePublishedFundingCsvJobs(publishedFundingCsvJobsRequest);
            await CreatePublishedOrganisationGroupCsvJobs(publishedFundingCsvJobsRequest);

            if (publishedFundingCsvJobsRequest.IsSpecificationSelectedForFunding)
            {
                await CreatePublishedGroupsCsvJob(publishedFundingCsvJobsRequest);
            }
        }

        public override bool IsForAction(GeneratePublishingCsvJobsCreationAction action)
        {
            return action == GeneratePublishingCsvJobsCreationAction.Release;
        }
    }
}
