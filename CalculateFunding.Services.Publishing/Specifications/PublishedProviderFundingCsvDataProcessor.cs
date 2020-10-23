using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderFundingCsvDataProcessor : IPublishedProviderFundingCsvDataProcessor
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly AsyncPolicy _publishedFundingPolicy;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly ILogger _logger;

        public PublishedProviderFundingCsvDataProcessor(IProducerConsumerFactory producerConsumerFactory,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _producerConsumerFactory = producerConsumerFactory;
            _publishedFunding = publishedFunding;
            _logger = logger;
            _publishedFundingPolicy = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IEnumerable<PublishedProviderFundingCsvData>> GetFundingData(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNotEmpty(publishedProviderIds, nameof(publishedProviderIds));
            Guard.IsNotEmpty(statuses, nameof(statuses));

            PublishedProviderFundingCsvDataProcessorContext context = new PublishedProviderFundingCsvDataProcessorContext(publishedProviderIds,
                statuses,
                specificationId);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProducePublishedProviderIds,
                GetFundingForPublishedProviderIds,
                20,
                5,
                _logger);

            await producerConsumer.Run(context);

            return context.GetData();
        }

        private Task<(bool isComplete, IEnumerable<string> items)> ProducePublishedProviderIds(CancellationToken token,
            dynamic context)
        {
            PublishedProviderFundingCsvDataProcessorContext countContext = (PublishedProviderFundingCsvDataProcessorContext)context;

            while (countContext.HasPages)
            {
                return Task.FromResult((false, countContext.NextPage().AsEnumerable()));
            }

            return Task.FromResult((true, ArraySegment<string>.Empty.AsEnumerable()));
        }

        private async Task GetFundingForPublishedProviderIds(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<string> items)
        {
            PublishedProviderFundingCsvDataProcessorContext countContext = (PublishedProviderFundingCsvDataProcessorContext)context;

            IEnumerable<PublishedProviderFundingCsvData> fundings = await _publishedFundingPolicy.ExecuteAsync(() => _publishedFunding.GetPublishedProvidersFundingDataForCsvReport(items,
                countContext.SpecificationId,
                countContext.Statuses));

            countContext.AddFundings(fundings);
        }

        private class PublishedProviderFundingCsvDataProcessorContext : PagedContext<string>
        {
            private readonly ConcurrentBag<PublishedProviderFundingCsvData> _fundings = new ConcurrentBag<PublishedProviderFundingCsvData>();

            public PublishedProviderFundingCsvDataProcessorContext(IEnumerable<string> items,
                PublishedProviderStatus[] statuses,
                string specificationId)
                : base(items, 100)
            {
                Statuses = statuses;
                SpecificationId = specificationId;
            }

            public string SpecificationId { get; }

            public PublishedProviderStatus[] Statuses { get; }

            public void AddFundings(IEnumerable<PublishedProviderFundingCsvData> fundings)
            {
                foreach (PublishedProviderFundingCsvData funding in fundings)
                {
                    _fundings.Add(funding);
                }
            }

            public IEnumerable<PublishedProviderFundingCsvData> GetData() => _fundings;
        }
    }
}