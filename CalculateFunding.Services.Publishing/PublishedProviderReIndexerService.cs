using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderReIndexerService : JobProcessingService, IPublishedProviderReIndexerService
    {
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly AsyncPolicy _publishedFundingResilience;
        private readonly ILogger _logger;

        private const int BatchSize = 1000;
        private const int IndexPageSize = 200;

        public PublishedProviderReIndexerService(
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingRepository publishedFundingRepository,
            IProducerConsumerFactory producerConsumerFactory,
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedProviderIndexerService = publishedProviderIndexerService;
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingResilience = publishingResiliencePolicies.PublishedFundingRepository;
            _producerConsumerFactory = producerConsumerFactory;
            _logger = logger;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference user = message.GetUserDetails();

            if (user != null)
            {
                _logger.Information($"{nameof(PublishedProviderReIndexerService)} initiated by: '{user.Name}'");
            }

            string specificationId = message.GetUserProperty<string>("specification-id");

            await _publishedFundingResilience.ExecuteAsync(() => _publishedFundingRepository.AllPublishedProviderBatchProcessing(async providerVersions =>
            {
                IEnumerable<PublishedProviderVersion> versions = providerVersions.Select(_ => _.Current);

                PagedContext<PublishedProviderVersion> context = new PagedContext<PublishedProviderVersion>(versions, IndexPageSize);

                IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(
                ProducePublishedProviderVersions,
                IndexPublishedProviders,
                10,
                4,
                _logger);

                await producerConsumer.Run(context);
            },
            BatchSize,
            specificationId));
        }

        private Task<(bool isComplete, IEnumerable<PublishedProviderVersion> items)> ProducePublishedProviderVersions(CancellationToken cancellationToken,
            dynamic context)
        {
            PagedContext<PublishedProviderVersion> publishedProviderVersionsContext = context as PagedContext<PublishedProviderVersion>;

            while (publishedProviderVersionsContext.HasPages)
            {
                return Task.FromResult((false, publishedProviderVersionsContext.NextPage().AsEnumerable()));
            }

            return Task.FromResult((true, ArraySegment<PublishedProviderVersion>.Empty.AsEnumerable()));
        }

        private async Task IndexPublishedProviders(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<PublishedProviderVersion> versions)
        {
            await _publishedProviderIndexerService.IndexPublishedProviders(versions);
        }
    }
}