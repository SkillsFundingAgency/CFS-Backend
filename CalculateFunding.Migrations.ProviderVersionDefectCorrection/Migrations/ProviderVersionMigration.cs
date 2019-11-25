using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Migrations.ProviderVersionDefectCorrection.Migrations
{
    public class ProviderVersionMigration : IProviderVersionMigration
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly Policy _resiliencePolicy;
        private readonly ILogger _logger;

        public ProviderVersionMigration(ICosmosRepository cosmosRepository,
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
                                    WHERE c.documentType = 'PublishedProviderVersion' 
                                    AND c.deleted = false
                                    AND (c.content.status = 'Approved' OR c.content.status = 'Draft') 
                                    AND c.content.majorVersion = 1 
                                    AND c.content.minorVersion = 0"
                };

                int batchCount = 1;

                await _resiliencePolicy.ExecuteAsync(() => _cosmosRepository.DocumentsBatchProcessingAsync<DocumentEntity<PublishedProviderVersion>>(async providerVersions =>
                    {
                        foreach (var providerVersion in providerVersions)
                        {
                            providerVersion.Content.MajorVersion = 0;
                            providerVersion.Content.MinorVersion = 1;
                        }

                        _logger.Information(
                            $"Bulk upserting batch number {batchCount} of provider version migration. Total provider versions migrated will be {batchCount * 10}.");

                        await _cosmosRepository.BulkUpsertAsync(providerVersions.Select(_ => _.Content).ToArray(), 1);

                        batchCount++;
                    },
                    query,
                    10));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to complete provider version migration");

                throw;
            }
        }
    }
}