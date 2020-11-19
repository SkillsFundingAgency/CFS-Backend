using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Options;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine
{
    public class CalculationAggregationService : ICalculationAggregationService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly IDatasetAggregationsRepository _datasetAggregationsRepository;
        private readonly EngineSettings _engineSettings;
        private readonly AsyncPolicy _cacheProviderPolicy;

        public CalculationAggregationService(
            ICacheProvider cacheProvider,
            IDatasetAggregationsRepository datasetAggregationsRepository,
            EngineSettings engineSettings,
            ICalculatorResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(datasetAggregationsRepository, nameof(datasetAggregationsRepository));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.CacheProvider, nameof(resiliencePolicies.CacheProvider));

            _cacheProvider = cacheProvider;
            _datasetAggregationsRepository = datasetAggregationsRepository;
            _engineSettings = engineSettings;
            _cacheProviderPolicy = resiliencePolicies.CacheProvider;
        }

        public async Task<IEnumerable<CalculationAggregation>> BuildAggregations(BuildAggregationRequest aggreagationRequest)
        {
            IEnumerable<CalculationAggregation> aggregations = Enumerable.Empty<CalculationAggregation>();

            aggregations = await _cacheProvider.GetAsync<List<CalculationAggregation>>(
                $"{ CacheKeys.DatasetAggregationsForSpecification}{aggreagationRequest.SpecificationId}");

            if (DoesNotExistInCache(aggregations))
            {
                aggregations = (await _datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId(aggreagationRequest.SpecificationId)).Select(m => new CalculationAggregation
                {
                    SpecificationId = m.SpecificationId,
                    Values = m.Fields.IsNullOrEmpty() ? Enumerable.Empty<AggregateValue>() : m.Fields.Select(f => new AggregateValue
                    {
                        AggregatedType = f.FieldType,
                        FieldDefinitionName = f.FieldDefinitionName,
                        Value = f.Value
                    })
                });

                await _cacheProvider.SetAsync($"{CacheKeys.DatasetAggregationsForSpecification}{aggreagationRequest.SpecificationId}", aggregations.ToList());
            }

            if (!aggreagationRequest.GenerateCalculationAggregationsOnly)
            {
                ConcurrentDictionary<string, List<decimal>> cachedCalculationAggregations = new ConcurrentDictionary<string, List<decimal>>(
                    StringComparer.InvariantCultureIgnoreCase);

                List<Task> allTasks = new List<Task>();
                SemaphoreSlim throttler = new SemaphoreSlim(_engineSettings.CalculationAggregationRetreivalParallelism);

                for (int i = 1; i <= aggreagationRequest.BatchCount; i++)
                {
                    await throttler.WaitAsync();

                    int currentBatchNumber = i;

                    allTasks.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                string batchedCacheKey = $"{CacheKeys.CalculationAggregations}{aggreagationRequest.SpecificationId}_{currentBatchNumber}";

                                Dictionary<string, List<decimal>> cachedCalculationAggregationsPart = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<Dictionary<string, List<decimal>>>(batchedCacheKey));

                                if (!cachedCalculationAggregationsPart.IsNullOrEmpty())
                                {
                                    foreach (KeyValuePair<string, List<decimal>> cachedAggregations in cachedCalculationAggregationsPart)
                                    {
                                        List<decimal> values = cachedCalculationAggregations.GetOrAdd(cachedAggregations.Key, new List<decimal>());

                                        values.AddRange(cachedAggregations.Value);
                                    }
                                }
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        }));
                }

                await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

                if (!cachedCalculationAggregations.IsNullOrEmpty())
                {
                    foreach (KeyValuePair<string, List<decimal>> cachedCalculationAggregation in cachedCalculationAggregations.OrderBy(o => o.Key))
                    {
                        aggregations = aggregations.Concat(new[]
                        {
                                new CalculationAggregation
                                {
                                    SpecificationId = aggreagationRequest.SpecificationId,
                                    Values = new []
                                    {
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Sum, Value = cachedCalculationAggregation.Value.Sum()},
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Min, Value = cachedCalculationAggregation.Value.Min()},
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Max, Value = cachedCalculationAggregation.Value.Max()},
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Average, Value = cachedCalculationAggregation.Value.Average()},
                                    }
                                }
                            });
                    }
                }
            }

            return aggregations;
        }

        private bool DoesNotExistInCache(IEnumerable<CalculationAggregation> aggregations)
        {
            return aggregations == null;
        }
    }
}
