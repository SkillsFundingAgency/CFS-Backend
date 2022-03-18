using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeletePublishedProvidersService : JobProcessingService, IDeletePublishedProvidersService
    {
        private readonly ICreateDeletePublishedProvidersJobs _jobs;
        private readonly IDeleteFundingSearchDocumentsService _deleteFundingSearchDocumentsService;
        private readonly IDeletePublishedFundingBlobDocumentsService _deletePublishedFundingBlobDocumentsService;
        private readonly IDeselectSpecificationForFundingService _deselectSpecificationForFundingService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly ILogger _logger;

        public DeletePublishedProvidersService(ICreateDeletePublishedProvidersJobs jobs,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishedFundingResilience,
            IJobManagement jobManagement,
            IDeleteFundingSearchDocumentsService deleteFundingSearchDocumentsService,
            IDeletePublishedFundingBlobDocumentsService deletePublishedFundingBlobDocumentsService,
            IDeselectSpecificationForFundingService deselectSpecificationForFundingService,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingResilience?.PublishedFundingRepository, nameof(publishedFundingResilience.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingResilience?.BlobClient, nameof(publishedFundingResilience.BlobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(deletePublishedFundingBlobDocumentsService, nameof(deletePublishedFundingBlobDocumentsService));
            Guard.ArgumentNotNull(deleteFundingSearchDocumentsService, nameof(deleteFundingSearchDocumentsService));
            Guard.ArgumentNotNull(deselectSpecificationForFundingService, nameof(deleteFundingSearchDocumentsService));

            _jobs = jobs;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _deselectSpecificationForFundingService = deselectSpecificationForFundingService;
            _deleteFundingSearchDocumentsService = deleteFundingSearchDocumentsService;
            _deletePublishedFundingBlobDocumentsService = deletePublishedFundingBlobDocumentsService;
            _publishedFundingRepositoryPolicy = publishedFundingResilience.PublishedFundingRepository;
        }

        public async Task<Job> QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            return await _jobs.CreateJob(fundingStreamId,
                 fundingPeriodId,
                 correlationId);
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string fundingPeriodId = message.GetUserProperty<string>("funding-period-id");
            string fundingStreamId = message.GetUserProperty<string>("funding-stream-id");

            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            _logger.Information($"Started delete published providers job for {fundingStreamId} {fundingPeriodId}");

            await DeletePublishedProviders(fundingStreamId, fundingPeriodId);
            await DeletePublishedProviderVersions(fundingStreamId, fundingPeriodId);
            await DeletePublishedFunding(fundingStreamId, fundingPeriodId);
            await DeletePublishedFundingVersions(fundingStreamId, fundingPeriodId);
            await DeletePublishedProviderSearchDocuments(fundingStreamId, fundingPeriodId);
            await DeletePublishedFundingBlobDocuments(fundingStreamId, fundingPeriodId, "publishedproviderversions");
            await DeletePublishedFundingBlobDocuments(fundingStreamId, fundingPeriodId, "publishedfunding");
            await DeletePublishedProviderFundingSearchDocuments(fundingStreamId, fundingPeriodId);

            await DeselectSpecificationForFunding(fundingStreamId, fundingPeriodId);
        }

        private async Task DeselectSpecificationForFunding(string fundingStreamId, string fundingPeriodId)
        {
            await _deselectSpecificationForFundingService.DeselectSpecificationForFunding(fundingStreamId, fundingPeriodId);
        }

        private async Task DeletePublishedFundingBlobDocuments(string fundingStreamId, string fundingPeriodId, string containerName)
        {
            await _deletePublishedFundingBlobDocumentsService.DeletePublishedFundingBlobDocuments(fundingStreamId, fundingPeriodId, containerName);
        }

        private async Task DeletePublishedProviderFundingSearchDocuments(string fundingStreamId, string fundingPeriodId)
        {
            await DeleteFundingSearchDocuments<PublishedFundingIndex>(fundingStreamId, fundingPeriodId);
        }

        private async Task DeletePublishedProviderSearchDocuments(string fundingStreamId, string fundingPeriodId)
        {
            await DeleteFundingSearchDocuments<PublishedProviderIndex>(fundingStreamId, fundingPeriodId);
        }

        private async Task DeletePublishedProviderVersions(string fundingStreamId, string fundingPeriodId)
        {
            _logger.Information($"Deleting published provider versions for {fundingStreamId} {fundingPeriodId}");

            await _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                _publishedFundingRepository.DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(fundingStreamId, fundingPeriodId));
        }

        private async Task DeletePublishedFunding(string fundingStreamId, string fundingPeriodId)
        {
            _logger.Information($"Deleting published funding for {fundingStreamId} {fundingPeriodId}");

            await _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                _publishedFundingRepository.DeleteAllPublishedFundingsByFundingStreamAndPeriod(fundingStreamId, fundingPeriodId));
        }

        private async Task DeletePublishedFundingVersions(string fundingStreamId, string fundingPeriodId)
        {
            _logger.Information($"Deleting published funding versions for {fundingStreamId} {fundingPeriodId}");

            await _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                _publishedFundingRepository.DeleteAllPublishedFundingVersionsByFundingStreamAndPeriod(fundingStreamId, fundingPeriodId));
        }

        private async Task DeletePublishedProviders(string fundingStreamId, string fundingPeriodId)
        {
            _logger.Information($"Deleting published providers for {fundingStreamId} {fundingPeriodId}");

            await _publishedFundingRepositoryPolicy.ExecuteAsync(() =>
                _publishedFundingRepository.DeleteAllPublishedProvidersByFundingStreamAndPeriod(fundingStreamId, fundingPeriodId));
        }

        private async Task DeleteFundingSearchDocuments<TIndex>(string fundingStreamId, string fundingPeriodId)
            where TIndex : class
        {
            await _deleteFundingSearchDocumentsService.DeleteFundingSearchDocuments<TIndex>(fundingStreamId, fundingPeriodId);
        }
    }
}