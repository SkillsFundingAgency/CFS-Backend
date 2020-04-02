using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Migrations.PublishedProviderPopulateReleased.Migrations
{
    public class PublishedProviderMigration : IPublishedProviderMigration
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly AsyncPolicy _resiliencePolicy;
        private readonly ILogger _logger;

        public PublishedProviderMigration(ICosmosRepository cosmosRepository,
           IPublishingResiliencePolicies resiliencePolicies,
           ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedProviderVersionRepository, nameof(resiliencePolicies.PublishedProviderVersionRepository));

            _cosmosRepository = cosmosRepository;
            _resiliencePolicy = resiliencePolicies.PublishedProviderVersionRepository;
            _logger = logger;
        }

        public async Task Run()
        {
            try
            {                

                CosmosDbQuery query = new CosmosDbQuery
                {
                    QueryText = @"  SELECT * 
                                    FROM c 
                                    WHERE c.documentType = 'PublishedProvider' 
                                    AND c.content.current.status = 'Released'
                                    AND IS_DEFINED(c.content.current.released) = false"
                };
                int batchCount = 1;

                await _resiliencePolicy.ExecuteAsync(() => _cosmosRepository.DocumentsBatchProcessingAsync<DocumentEntity<PublishedProvider>>(async publishedProviders =>
                {
                    List<DocumentEntity<PublishedProvider>> listPublishedProviders = new List<DocumentEntity<PublishedProvider>>();

                    foreach (var publishedProvider in publishedProviders)
                    {
                        if(publishedProvider.Content.Current.Status != PublishedProviderStatus.Released)
                        {
                            continue;
                        }
                        publishedProvider.Content.Released = publishedProvider.Content.Current;
                        listPublishedProviders.Add(publishedProvider);
                    }

                    _logger.Information(
                           $"Bulk upserting batch number {batchCount} of published provider release property update migration. Total published provider migrated will be {batchCount * 10}.");

                    await _cosmosRepository.BulkUpsertAsync(listPublishedProviders.Select(_ => _.Content).ToArray(), 1);
                    batchCount++;

                },
                    query,
                    10));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to complete published provider release property update migration");

                throw;
            }
        }
    }
}
