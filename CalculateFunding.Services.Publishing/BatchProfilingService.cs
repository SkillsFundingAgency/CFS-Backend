using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class BatchProfilingService : IBatchProfilingService
    {
        private readonly IProfilingApiClient _profiling;
        private readonly AsyncPolicy _profilingResilience;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly IBatchProfilingOptions _options;
        private readonly ILogger _logger;

        public BatchProfilingService(IProfilingApiClient profiling,
            IProducerConsumerFactory producerConsumerFactory,
            IBatchProfilingOptions options,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(profiling, nameof(profiling));
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(options, nameof(options));
            Guard.ArgumentNotNull(resiliencePolicies?.ProfilingApiClient, nameof(resiliencePolicies.ProfilingApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _profiling = profiling;
            _producerConsumerFactory = producerConsumerFactory;
            _options = options;
            _logger = logger;
            _profilingResilience = resiliencePolicies.ProfilingApiClient;
        }

        public async Task ProfileBatches(IBatchProfilingContext batchProfilingContext)
        {
            Guard.ArgumentNotNull(batchProfilingContext, nameof(batchProfilingContext));

            batchProfilingContext.InitialiseItems(1, _options.BatchSize);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceBatchProfileRequests,
                ProfileBatchRequests,
                10,
                _options.ConsumerCount,
                _logger);

            await producerConsumer.Run(batchProfilingContext);
        }

        private Task<(bool isComplete, IEnumerable<BatchProfilingRequestModel>)> ProduceBatchProfileRequests(CancellationToken token,
            dynamic context)
        {
            IBatchProfilingContext batchProfileRequestContext = (IBatchProfilingContext)context;

            while (batchProfileRequestContext.HasPages)
            {
                return Task.FromResult((false, batchProfileRequestContext.NextPage().AsEnumerable()));
            }

            return Task.FromResult((true, ArraySegment<BatchProfilingRequestModel>.Empty.AsEnumerable()));
        }

        private async Task ProfileBatchRequests(CancellationToken token,
            dynamic context,
            IEnumerable<BatchProfilingRequestModel> batchProfilingRequests)
        {
            IBatchProfilingContext batchProfileRequestContext = (IBatchProfilingContext)context;

            foreach (BatchProfilingRequestModel request in batchProfilingRequests)
            {
                ValidatedApiResponse<IEnumerable<BatchProfilingResponseModel>> response = await _profilingResilience.ExecuteAsync(()
                    => _profiling.GetBatchProfilePeriods(request));

                IEnumerable<BatchProfilingResponseModel> batchProfilingResponseModels = response?.Content;

                if (batchProfilingResponseModels == null)
                {
                    throw new NonRetriableException(
                        $"Unable to complete batch profiling. Missing results or configuration from profiling service for {request.FundingStreamId} {request.FundingPeriodId} while processing funding line '{request.FundingLineCode}' for Provider Type='{request.ProviderType}' and Provider Sub Type '{request.ProviderSubType}'");
                }

                foreach (BatchProfilingResponseModel batchProfilingResponse in batchProfilingResponseModels)
                {
                    batchProfileRequestContext.ReconcileBatchProfilingResponse(batchProfilingResponse);
                }
            }
        }
    }
}