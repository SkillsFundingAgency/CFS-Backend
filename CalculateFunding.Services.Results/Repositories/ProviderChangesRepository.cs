using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Results.Repositories
{
    public class ProviderChangesRepository : IProviderChangesRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepo;
        private readonly ILogger _logger;

        public ProviderChangesRepository(ICosmosRepository cosmosRepository, ILogger logger)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cosmosRepo = cosmosRepository;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) cosmosRepoHealth = await _cosmosRepo.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderSourceDatasetRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepo.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public async Task AddProviderChanges(IEnumerable<ProviderChangeRecord> providerChangeRecords)
        {
            if (!providerChangeRecords.AnyWithNullCheck())
            {
                return;
            }

            List<Task> allTasks = new List<Task>(providerChangeRecords.Count());
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (ProviderChangeRecord changeRecord in providerChangeRecords)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _cosmosRepo.UpsertAsync(changeRecord, changeRecord.ProviderId);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error while inserting provider change records to cosmos. ProviderId = {ProviderId}", changeRecord.ProviderId);
                            throw;
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

        }
    }
}
