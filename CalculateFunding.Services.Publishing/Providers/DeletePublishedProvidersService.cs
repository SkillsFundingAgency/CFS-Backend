using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeletePublishedProvidersService : IDeletePublishedProvidersService
    {
        private readonly ICreateDeletePublishedProvidersJobs _jobs;
        private readonly IDeleteFundingSearchDocumentsService _deleteFundingSearchDocumentsService;
        private readonly IDeletePublishedFundingBlobDocumentsService _deletePublishedFundingBlobDocumentsService;
        private readonly IDeselectSpecificationForFundingService _deselectSpecificationForFundingService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;

        public DeletePublishedProvidersService(ICreateDeletePublishedProvidersJobs jobs,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishedFundingResilience,
            IJobManagement jobManagement,
            IDeleteFundingSearchDocumentsService deleteFundingSearchDocumentsService,
            IDeletePublishedFundingBlobDocumentsService deletePublishedFundingBlobDocumentsService,
            IDeselectSpecificationForFundingService deselectSpecificationForFundingService,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingResilience?.PublishedFundingRepository, nameof(publishedFundingResilience.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingResilience?.BlobClient, nameof(publishedFundingResilience.BlobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(deletePublishedFundingBlobDocumentsService, nameof(deletePublishedFundingBlobDocumentsService));
            Guard.ArgumentNotNull(deleteFundingSearchDocumentsService, nameof(deleteFundingSearchDocumentsService));
            Guard.ArgumentNotNull(deselectSpecificationForFundingService, nameof(deleteFundingSearchDocumentsService));

            _jobs = jobs;
            _publishedFundingRepository = publishedFundingRepository;
            _logger = logger;
            _deselectSpecificationForFundingService = deselectSpecificationForFundingService;
            _deleteFundingSearchDocumentsService = deleteFundingSearchDocumentsService;
            _deletePublishedFundingBlobDocumentsService = deletePublishedFundingBlobDocumentsService;
            _jobManagement = jobManagement;
            _publishedFundingRepositoryPolicy = publishedFundingResilience.PublishedFundingRepository;
        }

        public async Task QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            string correlationId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            await _jobs.CreateJob(fundingStreamId,
                fundingPeriodId,
                correlationId);
        }

        public async Task DeletePublishedProvidersJob(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string fundingPeriodId = message.GetUserProperty<string>("funding-period-id");
            string fundingStreamId = message.GetUserProperty<string>("funding-stream-id");
            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            try
            {
                await CheckJobStatus(jobId);

                _logger.Information($"Started delete published providers job for {fundingStreamId} {fundingPeriodId}");

                await TaskHelper.WhenAllAndThrow(DeletePublishedProviders(fundingStreamId, fundingPeriodId),
                DeletePublishedProviderVersions(fundingStreamId, fundingPeriodId),
                DeletePublishedFunding(fundingStreamId, fundingPeriodId),
                DeletePublishedFundingVersions(fundingStreamId, fundingPeriodId),
                DeletePublishedProviderSearchDocuments(fundingStreamId, fundingPeriodId),
                DeletePublishedFundingBlobDocuments(fundingStreamId, fundingPeriodId, "publishedproviderversions"),
                DeletePublishedFundingBlobDocuments(fundingStreamId, fundingPeriodId, "publishedfunding"),
                DeletePublishedProviderFundingSearchDocuments(fundingStreamId, fundingPeriodId));

                await DeselectSpecificationForFunding(fundingStreamId, fundingPeriodId);

                _logger.Information($"Completed delete published providers job for {fundingStreamId} {fundingPeriodId}");

                await TrackJobCompleted(jobId);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unable to complete delete published providers job");

                await TrackJobFailed(jobId, exception);

                throw new NonRetriableException("Unable to delete published providers.", exception);
            }
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

        private async Task CheckJobStatus(string jobId)
        {
            await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
        }

        private async Task TrackJobFailed(string jobId, Exception exception)
        {
            await AddJobTracking(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = exception.ToString()
            });
        }

        private async Task TrackJobCompleted(string jobId)
        {
            await AddJobTracking(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = true
            });
        }

        private async Task AddJobTracking(string jobId, JobLogUpdateModel tracking)
        {
            await _jobManagement.AddJobLog(jobId, tracking);
        }
    }
}