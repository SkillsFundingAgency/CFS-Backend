using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class PublishedProviderLookupService : IPublishedProviderLookupService
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly AsyncPolicy _publishedFundingPolicy;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly ILogger _logger;

        public PublishedProviderLookupService(IProducerConsumerFactory producerConsumerFactory,
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
            _publishedFundingPolicy = resiliencePolicies.PublishedFundingRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<PublishedProviderFundingSummary>> GetPublishedProviderFundingSummaries(
            SpecificationSummary specificationSummary,
            PublishedProviderStatus[] statuses,
            IEnumerable<string> publishedProviderIds = null)
        {
            if (publishedProviderIds.IsNullOrEmpty())
            {
                publishedProviderIds = await _publishedFundingPolicy.ExecuteAsync(() => _publishedFunding.GetPublishedProviderPublishedProviderIds(specificationSummary.Id));
            }

            PublishedProviderFundingSummaryProcessorContext context = new PublishedProviderFundingSummaryProcessorContext(
                publishedProviderIds,
                statuses,
                specificationSummary.Id);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceFundingSummaryPublishedProviderIds,
                GetReleaseFundingSummaryForPublishedProviderIds,
                20,
                5,
                _logger);

            await producerConsumer.Run(context);

            return context.PublishedProviderFundingSummaries;
        }

        private Task<(bool isComplete, IEnumerable<string> items)> ProduceFundingSummaryPublishedProviderIds(CancellationToken token,
            dynamic context)
        {
            PublishedProviderFundingSummaryProcessorContext countContext = (PublishedProviderFundingSummaryProcessorContext)context;

            while (countContext.HasPages)
            {
                return Task.FromResult((false, countContext.NextPage().AsEnumerable()));
            }

            return Task.FromResult((true, ArraySegment<string>.Empty.AsEnumerable()));
        }

        private async Task GetReleaseFundingSummaryForPublishedProviderIds(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<string> items)
        {
            PublishedProviderFundingSummaryProcessorContext countContext = (PublishedProviderFundingSummaryProcessorContext)context;

            IEnumerable<PublishedProviderFundingSummary> fundings = await _publishedFundingPolicy.ExecuteAsync(() => _publishedFunding.GetReleaseFundingPublishedProviders(items,
                countContext.SpecificationId,
                countContext.Statuses));

            countContext.AddFundings(fundings);
        }

        private class PublishedProviderFundingSummaryProcessorContext : PagedContext<string>
        {
            private readonly ConcurrentBag<PublishedProviderFundingSummary> _fundings = new ConcurrentBag<PublishedProviderFundingSummary>();

            public PublishedProviderFundingSummaryProcessorContext(IEnumerable<string> items,
                PublishedProviderStatus[] statuses,
                string specificationId)
                : base(items, 100)
            {
                Statuses = statuses;
                SpecificationId = specificationId;
            }

            public string SpecificationId { get; }

            public PublishedProviderStatus[] Statuses { get; }

            public void AddFundings(IEnumerable<PublishedProviderFundingSummary> fundings)
            {
                foreach (PublishedProviderFundingSummary funding in fundings)
                {
                    _fundings.Add(funding);
                }
            }

            public IEnumerable<PublishedProviderFundingSummary> PublishedProviderFundingSummaries => _fundings.AsEnumerable();
        }
    }
}
