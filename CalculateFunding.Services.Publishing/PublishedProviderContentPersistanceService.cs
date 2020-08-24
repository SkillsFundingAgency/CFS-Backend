using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderContentPersistanceService : IPublishedProviderContentPersistanceService
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPublishedProviderVersioningService _publishedProviderVersioningService;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedProviderContentPersistanceService(
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedProviderVersioningService publishedProviderVersioningService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            ILogger logger,
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(publishedProviderVersioningService, nameof(publishedProviderVersioningService));

            _publishedProviderIndexerService = publishedProviderIndexerService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _publishedProviderVersioningService = publishedProviderVersioningService;
            _logger = logger;
            _publishingEngineOptions = publishingEngineOptions;
        }

        public async Task SavePublishedProviderContents(TemplateMetadataContents templateMetadataContents, TemplateMapping templateMapping, IEnumerable<PublishedProvider> publishedProvidersToUpdate, IPublishedProviderContentsGenerator generator, bool publishAll = false)
        {
            _logger.Information("Saving published provider contents");
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.SavePublishedProviderContentsConcurrencyCount);
            foreach (PublishedProvider provider in publishedProvidersToUpdate)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            IEnumerable<PublishedProviderVersion> publishedProviderVersions = publishAll ? 
                                await _publishedProviderVersioningService.GetVersions(provider) :  
                                new[] { provider.Current };

                            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
                            {
                                string contents = generator.GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping);

                                if (string.IsNullOrWhiteSpace(contents))
                                {
                                    throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedProviderVersion.Id}'");
                                }

                                try
                                {
                                    await _publishedProviderVersionService.SavePublishedProviderVersionBody(
                                        publishedProviderVersion.FundingId, contents, publishedProviderVersion.SpecificationId);
                                }
                                catch (Exception ex)
                                {
                                    throw new RetriableException(ex.Message);
                                }

                                try
                                {
                                    await _publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);
                                }
                                catch (Exception ex)
                                {
                                    throw new RetriableException(ex.Message);
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
        }
    }
}
