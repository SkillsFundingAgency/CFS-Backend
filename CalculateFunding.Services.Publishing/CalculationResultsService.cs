using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationResultsService : ICalculationResultsService
    {
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly ICalculationResultsRepository _calculationResultsRepository;
        private readonly ILogger _logger;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public CalculationResultsService(
            IPublishingResiliencePolicies resiliencePolicies,
            ICalculationResultsRepository calculationResultsRepository,
            ILogger logger, 
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(calculationResultsRepository, nameof(calculationResultsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _resultsRepositoryPolicy = resiliencePolicies.CalculationResultsRepository;
            _calculationResultsRepository = calculationResultsRepository;
            _logger = logger;
            _publishingEngineOptions = publishingEngineOptions;
        }

        public async Task<IDictionary<string, ProviderCalculationResult>> GetCalculationResultsBySpecificationId(string specificationId, IEnumerable<string> scopedProviderIds)
        {
            ConcurrentDictionary<string, ProviderCalculationResult> results = new ConcurrentDictionary<string, ProviderCalculationResult>();
            if (scopedProviderIds.AnyWithNullCheck())
            {
                List<string> providerIds = new List<string>(scopedProviderIds.Distinct());

                List<Task> allTasks = new List<Task>();
                SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.GetCalculationResultsConcurrencyCount);
                foreach (string providerId in providerIds)
                {
                    await throttler.WaitAsync();
                    allTasks.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                ProviderCalculationResult result = await _resultsRepositoryPolicy.ExecuteAsync(() => _calculationResultsRepository.GetCalculationResultsBySpecificationAndProvider(specificationId, providerId));
                                results.TryAdd(providerId, result);
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        }));
                }
                await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
            }

            return results;
        }
    }
}
