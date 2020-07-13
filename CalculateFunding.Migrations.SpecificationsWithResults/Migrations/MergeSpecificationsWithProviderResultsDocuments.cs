using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Migrations.SpecificationsWithResults.Migrations
{
    public class MergeSpecificationsWithProviderResultsDocuments : IMergeSpecificationsWithProviderResultsDocuments
    {
        private readonly ConcurrentDictionary<string, SpecificationSummary> _cachedSpecifications = new ConcurrentDictionary<string, SpecificationSummary>();

        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly IPoliciesApiClient _policies;
        private readonly AsyncPolicy _policiesPolicy;
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ISpecificationsWithProviderResultsService _specificationsWithProviderResults;
        private readonly ISpecificationsApiClient _specifications;
        private readonly ILogger _logger;

        public MergeSpecificationsWithProviderResultsDocuments(ICosmosRepository cosmosRepository,
            ILogger logger,
            ISpecificationsWithProviderResultsService specificationsWithProviderResults,
            ISpecificationsApiClient specifications,
            IProducerConsumerFactory producerConsumerFactory,
            IResultsResiliencePolicies resultsResiliencePolicies,
            IPoliciesApiClient policies)
        {
            _cosmosRepository = cosmosRepository;
            _logger = logger;
            _specificationsWithProviderResults = specificationsWithProviderResults;
            _specifications = specifications;
            _producerConsumerFactory = producerConsumerFactory;
            _policies = policies;
            _policiesPolicy = resultsResiliencePolicies.PoliciesApiClient;
        }

        public async Task Run()
        {
            try
            {
                CosmosDbQuery query = new CosmosDbQuery
                {
                    QueryText = @"  SELECT * 
                                    FROM c 
                                    WHERE c.documentType = 'ProviderResult' 
                                    AND c.deleted = false"
                };

                ICosmosDbFeedIterator<ProviderResult> feed = _cosmosRepository.GetFeedIterator<ProviderResult>(query);
                
                ApiResponse<IEnumerable<FundingPeriod>> fundingPeriods = await _policiesPolicy.ExecuteAsync(() => _policies.GetFundingPeriods());

                MergeContext context = new MergeContext(feed, fundingPeriods.Content);

                IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceSpecificationInformation,
                    ConsumeSpecificationInformation,
                    8,
                    4,
                    _logger);

                await producerConsumer.Run(context);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to complete provider version migration");

                throw;
            }
        }

        private async Task<(bool Complete, IEnumerable<MergeSpecificationRequest> items)> ProduceSpecificationInformation(CancellationToken cancellationToken,
            dynamic context)
        {
            ICosmosDbFeedIterator<ProviderResult> feed = ((MergeContext) context).Feed;

            while (feed.HasMoreResults)
            {
                ProviderResult[] providerResults = (await feed.ReadNext(cancellationToken)).ToArray();

                Console.WriteLine($"Processing next {providerResults.Length} provider results");

                MergeSpecificationRequest[] requests = providerResults.Select(_ =>
                {
                    SpecificationSummary specificationSummary = GetSpecificationSummary(_.SpecificationId);

                    Console.WriteLine($"Creating merge specification request for provider {_.Provider.Id} and specification {_.SpecificationId}");

                    return new MergeSpecificationRequest(new SpecificationInformation
                        {
                            Id = specificationSummary.Id,
                            Name = specificationSummary.Name,
                            FundingPeriodId = specificationSummary.FundingPeriod.Id,
                            LastEditDate = specificationSummary.LastEditedDate
                        },
                        _.Provider.Id);
                }).ToArray();

                return (false, requests);
            }

            return (true, ArraySegment<MergeSpecificationRequest>.Empty);
        }

        private async Task ConsumeSpecificationInformation(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<MergeSpecificationRequest> items)
        {
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods = ((MergeContext) context).FundingPeriods;

            foreach (MergeSpecificationRequest mergeSpecificationRequest in items)
            {
                _logger.Information($"Merging specification information {mergeSpecificationRequest.SpecificationInformation.Name} for provider id {mergeSpecificationRequest.ProviderId}");

                await _specificationsWithProviderResults.MergeSpecificationInformation(mergeSpecificationRequest.SpecificationInformation,
                    mergeSpecificationRequest.ProviderId,
                    fundingPeriods);
            }
        }

        private SpecificationSummary GetSpecificationSummary(string specificationId)
        {
            return _cachedSpecifications.GetOrAdd(specificationId,
                id => _specifications.GetSpecificationSummaryById(id)
                    .GetAwaiter()
                    .GetResult()
                    .Content);
        }

        private class MergeContext
        {
            public MergeContext(ICosmosDbFeedIterator<ProviderResult> feed,
                IEnumerable<FundingPeriod> fundingPeriods)
            {
                Feed = feed;
                FundingPeriods = new ConcurrentDictionary<string, FundingPeriod>(
                    fundingPeriods.ToDictionary(_ => _.Id));
            }

            public ICosmosDbFeedIterator<ProviderResult> Feed { get; }

            public ConcurrentDictionary<string, FundingPeriod> FundingPeriods { get; }
        }
    }

    public class MergeSpecificationRequest
    {
        public MergeSpecificationRequest(SpecificationInformation specificationInformation,
            string providerId)
        {
            SpecificationInformation = specificationInformation;
            ProviderId = providerId;
        }

        public SpecificationInformation SpecificationInformation { get; }

        public string ProviderId { get; }
    }
}