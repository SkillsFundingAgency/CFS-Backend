using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PostReleaseJobCreationService : IPostReleaseJobCreationService
    {
        private readonly IPublishedFundingCsvJobsService _publishedFundingCsvJobsService;
        private readonly ICreatePublishDatasetsDataCopyJob _createPublishDatasetsDataCopyJob;
        private readonly ICreateProcessDatasetObsoleteItemsJob _createProcessDatasetObsoleteItemsJob;

        public PostReleaseJobCreationService(IPublishedFundingCsvJobsService publishedFundingCsvJobsService,
            ICreatePublishDatasetsDataCopyJob createPublishDatasetsDataCopyJob,
            ICreateProcessDatasetObsoleteItemsJob createProcessDatasetObsoleteItemsJob)
        {
            Guard.ArgumentNotNull(publishedFundingCsvJobsService, nameof(publishedFundingCsvJobsService));
            Guard.ArgumentNotNull(createPublishDatasetsDataCopyJob, nameof(createPublishDatasetsDataCopyJob));
            Guard.ArgumentNotNull(createProcessDatasetObsoleteItemsJob, nameof(createProcessDatasetObsoleteItemsJob));

            _publishedFundingCsvJobsService = publishedFundingCsvJobsService;
            _createPublishDatasetsDataCopyJob = createPublishDatasetsDataCopyJob;
            _createProcessDatasetObsoleteItemsJob = createProcessDatasetObsoleteItemsJob;
        }
        public async Task QueueJobs(SpecificationSummary specification, string correlationId, Reference author)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            await _publishedFundingCsvJobsService.GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction.Release,
                specification.Id,
                specification.FundingPeriod.Id,
                specification.FundingStreams.Select(fs => fs.Id),
                correlationId,
                author);

            await _createPublishDatasetsDataCopyJob.CreateJob(specification.Id, author, correlationId);

            await _createProcessDatasetObsoleteItemsJob.CreateJob(specification.Id, author, correlationId);
        }
    }
}
