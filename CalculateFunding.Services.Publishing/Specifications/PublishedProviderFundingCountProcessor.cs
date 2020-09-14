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
    public class PublishedProviderFundingCountProcessor : IPublishedProviderFundingCountProcessor
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly AsyncPolicy _publishedFundingPolicy;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly ILogger _logger;

        public PublishedProviderFundingCountProcessor(IProducerConsumerFactory producerConsumerFactory,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies  resiliencePolicies,
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

        public async Task<PublishedProviderFundingCount> GetFundingCount(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNotEmpty(publishedProviderIds, nameof(publishedProviderIds));
            Guard.IsNotEmpty(statuses, nameof(statuses));
            
            PublishedProviderFundingCountProcessorContext context = new PublishedProviderFundingCountProcessorContext(publishedProviderIds, 
                statuses, 
                specificationId);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProducePublishedProviderIds,
                GetFundingForPublishedProviderIds,
                20,
                5,
                _logger);

            await producerConsumer.Run(context);

            return context.GetTotal();
        }

        private Task<(bool isComplete, IEnumerable<string> items)> ProducePublishedProviderIds(CancellationToken token,
            dynamic context)
        {
            PublishedProviderFundingCountProcessorContext countContext = (PublishedProviderFundingCountProcessorContext) context;

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
            PublishedProviderFundingCountProcessorContext countContext = (PublishedProviderFundingCountProcessorContext) context;

            IEnumerable<PublishedProviderFunding> fundings = await _publishedFundingPolicy.ExecuteAsync(() => _publishedFunding.GetPublishedProvidersFunding(items,
                countContext.SpecificationId,
                countContext.Statuses));
            
            countContext.AddFundings(fundings);
        }

        private class PublishedProviderFundingCountProcessorContext : PagedContext<string>
        {
            private readonly ConcurrentBag<PublishedProviderFunding> _fundings = new ConcurrentBag<PublishedProviderFunding>();

            public PublishedProviderFundingCountProcessorContext(IEnumerable<string> items,
                PublishedProviderStatus[] statuses,
                string specificationId)
                : base(items, 100)
            {
                Statuses = statuses;
                SpecificationId = specificationId;
            }

            public string SpecificationId { get; }

            public PublishedProviderStatus[] Statuses { get; }

            public void AddFundings(IEnumerable<PublishedProviderFunding> fundings)
            {
                foreach (PublishedProviderFunding funding in fundings)
                {
                    _fundings.Add(funding);
                }
            }

            public PublishedProviderFundingCount GetTotal()
                => new PublishedProviderFundingCount
                {
                    Count = _fundings.Count(),
                    TotalFunding = _fundings.Sum(_ => _.TotalFunding.GetValueOrDefault(0)),
                    ProviderTypes = _fundings.Select(_ => _.ProviderTypeSubType).Distinct().ToList(),
                    FundingStreamsFundings = _fundings.GroupBy(x => x.FundingStreamId)
                                                    .Select(g => new PublishedProivderFundingStreamFunding() { FundingStreamId = g.Key, TotalFunding = g.Sum(_ => _.TotalFunding.GetValueOrDefault(0))})
                                                    .ToList(),
                    LocalAuthorities = _fundings.Select(_ => _.LaCode).Distinct().ToList()
                };
        }
    }
}