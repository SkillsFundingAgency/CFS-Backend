using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.CodeAnalysis.CSharp;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class SpecificationsWithProviderResultsService : ISpecificationsWithProviderResultsService
    {
        private readonly IPoliciesApiClient _policies;
        private readonly ICalculationResultsRepository _results;
        private readonly IJobManagement _jobs;
        private readonly AsyncPolicy _resultsPolicy;
        private readonly AsyncPolicy _jobsPolicy;
        private readonly AsyncPolicy _policiesPolicy;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ILogger _logger;

        public SpecificationsWithProviderResultsService(ICalculationResultsRepository results,
            IPoliciesApiClient policies,
            IJobManagement jobs,
            IProducerConsumerFactory producerConsumerFactory,
            IResultsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(results, nameof(results));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationProviderResultsSearchRepository, nameof(resiliencePolicies.CalculationProviderResultsSearchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _results = results;
            _jobs = jobs;
            _producerConsumerFactory = producerConsumerFactory;
            _resultsPolicy = resiliencePolicies.CalculationProviderResultsSearchRepository;
            _jobsPolicy = resiliencePolicies.JobsApiClient;
            _policiesPolicy = resiliencePolicies.PoliciesApiClient;
            _logger = logger;
            _policies = policies;
        }

        public async Task<IActionResult> QueueMergeSpecificationInformationJob(MergeSpecificationInformationRequest mergeRequest,
            Reference user,
            string correlationId)
        {
            Guard.ArgumentNotNull(mergeRequest, nameof(mergeRequest));

            JobCreateModel job = new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.MergeSpecificationInformationForProviderJob,
                InvokerUserId = user?.Id,
                InvokerUserDisplayName = user?.Name,
                CorrelationId = correlationId,
                Trigger = new Trigger
                {
                    Message = "Specification or Results change require merging specification information for providers with results",
                    EntityType = "Specification",
                    EntityId = mergeRequest.SpecificationInformation.Id
                },
                MessageBody = mergeRequest.AsJson()
            };

            return new OkObjectResult(await _jobsPolicy.ExecuteAsync(() => _jobs.QueueJob(job)));   
        }

        public async Task MergeSpecificationInformation(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = GetMessageProperty(message, "jobId");

            await StartTrackingJob(jobId);

            MergeSpecificationInformationRequest mergeRequest = message.GetPayloadAsInstanceOf<MergeSpecificationInformationRequest>();

            await MergeSpecificationInformation(mergeRequest);

            await CompleteJob(jobId);
        }

        public async Task MergeSpecificationInformation(MergeSpecificationInformationRequest mergeRequest,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods = null)
        {
            Guard.ArgumentNotNull(mergeRequest, nameof(mergeRequest));

            if (mergeRequest.IsForAllProviders)
            {
                await MergeSpecificationInformationForAllProviders(mergeRequest.SpecificationInformation);
            }
            else
            {
                await MergeSpecificationInformationForProviderBatch(mergeRequest.SpecificationInformation, 
                    mergeRequest.ProviderIds, 
                    fundingPeriods);   
            }
        }

        private async Task StartTrackingJob(string jobId)
            => await UpdateJobStatus(jobId);

        private async Task CompleteJob(string jobId)
            => await UpdateJobStatus(jobId, true);

        private async Task UpdateJobStatus(string jobId,
            bool? completed = null)
            => await _jobsPolicy.ExecuteAsync(() => _jobs.UpdateJobStatus(jobId,
                0,
                0,
                completed));

        private string GetMessageProperty(Message message,
            string key)
            => message.GetUserProperty<string>(key);

        private async Task MergeSpecificationInformationForAllProviders(SpecificationInformation specificationInformation)
        {
            string specificationId = specificationInformation.Id;

            LogInformation($"Merging specification information for specification {specificationId} into summary for all providers with results currently tracking it");

            ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> providersWithResultsForSpecifications = GetProviderWithResultsBySpecificationId(specificationId);

            await EnsureFundingPeriodEndDateQueried(specificationInformation);

            MergeSpecificationInformationContext context = new MergeSpecificationInformationContext(providersWithResultsForSpecifications, specificationInformation);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceProviderWithResultsForSpecifications,
                MergeSpecificationInformation,
                200,
                2,
                _logger);

            await producerConsumer.Run(context);
        }

        private async Task<(bool isComplete, IEnumerable<ProviderWithResultsForSpecifications> items)> ProduceProviderWithResultsForSpecifications(CancellationToken token,
            dynamic context)
        {
            ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> feedIterator = ((MergeSpecificationInformationContext) context).FeedIterator;

            while (feedIterator.HasMoreResults)
            {
                ProviderWithResultsForSpecifications[] page = (await feedIterator.ReadNext(token)).ToArray();

                LogInformation($"Producing next page of ProviderWithResultsForSpecifications with {page.Length} items");

                return (false, page);
            }

            return (true, ArraySegment<ProviderWithResultsForSpecifications>.Empty);
        }

        private async Task MergeSpecificationInformation(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<ProviderWithResultsForSpecifications> items)
        {
            SpecificationInformation specificationInformation = ((MergeSpecificationInformationContext) context).SpecificationInformation;

            LogInformation($"Merging SpecificationInformation for next {items.Count()} ProviderWithResultsForSpecifications");

            foreach (ProviderWithResultsForSpecifications providerWithResultsForSpecification in items)
            {
                providerWithResultsForSpecification.MergeSpecificationInformation(specificationInformation);

                LogInformation($"Merged specification information for {specificationInformation.Id} into ProviderWithResultsForSpecifications for provider {providerWithResultsForSpecification.Id}");
            }

            LogInformation($"Upserting page with {items.Count()} merged ProviderWithResultsForSpecifications for specification {specificationInformation.Id}");

            ProviderWithResultsForSpecifications[] dirtyItems = items.Where(_ => _.GetIsDirty()).ToArray();
            
            await _resultsPolicy.ExecuteAsync(() => _results.UpsertSpecificationWithProviderResults(dirtyItems));
        }

        private async Task MergeSpecificationInformationForProviderBatch(SpecificationInformation specificationInformation,
            IEnumerable<string> providerIds,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods)
        {
            LogInformation($"Merging specification information for specification {specificationInformation.Id} into summary for provider batch with length {providerIds.Count()}");
            
            foreach (string providerId in providerIds)
            {
                LogInformation($"Merging specification information for specification {specificationInformation.Id} into summary for provider {providerId}");

                ProviderWithResultsForSpecifications providerWithResultsForSpecifications = await GetProviderWithResultsByProviderId(providerId);

                providerWithResultsForSpecifications ??= new ProviderWithResultsForSpecifications
                {
                    Provider = new ProviderInformation
                    {
                        Id = providerId
                    }
                };

                await EnsureFundingPeriodEndDateQueried(specificationInformation, fundingPeriods);

                providerWithResultsForSpecifications.MergeSpecificationInformation(specificationInformation);

                if (providerWithResultsForSpecifications.GetIsDirty())
                {
                    await _results.UpsertSpecificationWithProviderResults(providerWithResultsForSpecifications);
                }
            }
            
            LogInformation($"Completed merging specification information for specification {specificationInformation.Id} into summary for provider batch with length {providerIds.Count()}");
        }

        public async Task<IActionResult> GetSpecificationsWithProviderResultsForProviderId(string providerId)
        {
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            ProviderWithResultsForSpecifications providerWithResultsForSpecifications = await GetProviderWithResultsByProviderId(providerId);

            if (providerWithResultsForSpecifications == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(providerWithResultsForSpecifications.Specifications?.ToArray() ?? new SpecificationInformation[0]);
        }

        private async Task EnsureFundingPeriodEndDateQueried(SpecificationInformation specificationInformation,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods = null)
        {
            if (specificationInformation.FundingPeriodEnd.HasValue)
            {
                LogInformation($"Specification funding period end date for information for {specificationInformation.Id} already present.");
            }

            LogInformation($"Querying funding period end date for information for {specificationInformation.Id}");

            specificationInformation.FundingPeriodEnd = await GetFundingPeriodEndDate(specificationInformation.FundingPeriodId,
                fundingPeriods);
        }

        private async Task<DateTimeOffset?> GetFundingPeriodEndDate(string fundingPeriodId,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods)
        {
            if (fundingPeriods == null)
            {
                return  (await GetFundingPeriod(fundingPeriodId))?.Content?.EndDate;
            }

            return fundingPeriods.GetOrAdd(fundingPeriodId,
                _ => GetFundingPeriod(fundingPeriodId)
                    .GetAwaiter()
                    .GetResult()?.Content)?.EndDate;
        }

        private async Task<ApiResponse<FundingPeriod>> GetFundingPeriod(string fundingPeriodId) 
            => await _policiesPolicy.ExecuteAsync(() => _policies.GetFundingPeriodById(fundingPeriodId));

        private Task<ProviderWithResultsForSpecifications> GetProviderWithResultsByProviderId(string providerId)
            => _resultsPolicy.ExecuteAsync(() => _results.GetProviderWithResultsForSpecificationsByProviderId(providerId));

        private ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> GetProviderWithResultsBySpecificationId(string specificationId) 
            => _results.GetProvidersWithResultsForSpecificationBySpecificationId(specificationId);

        private void LogInformation(string message) => _logger.Information(FormatLogMessage(message));

        private static string FormatLogMessage(string message) => $"SpecificationsWithProviderResultsService: {message}";

        private class MergeSpecificationInformationContext
        {
            public MergeSpecificationInformationContext(ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> feedIterator,
                SpecificationInformation specificationInformation)
            {
                FeedIterator = feedIterator;
                SpecificationInformation = specificationInformation;
            }

            public ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> FeedIterator { get; }

            public SpecificationInformation SpecificationInformation { get; }
        }
    }
}