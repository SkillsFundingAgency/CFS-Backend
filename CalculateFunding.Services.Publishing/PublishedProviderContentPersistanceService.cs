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
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;

        public PublishedProviderContentPersistanceService(
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));

            _publishedProviderIndexerService = publishedProviderIndexerService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _logger = logger;
        }

        public async Task SavePublishedProviderContents(TemplateMetadataContents templateMetadataContents, TemplateMapping templateMapping, Dictionary<string, GeneratedProviderResult> generatedPublishedProviderData, Dictionary<string, PublishedProvider> publishedProvidersToUpdate, IPublishedProviderContentsGenerator generator)
        {
            _logger.Information("Saving published provider contents");
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (KeyValuePair<string, PublishedProvider> provider in publishedProvidersToUpdate)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            PublishedProviderVersion publishedProviderVersion = provider.Value.Current;

                            string contents = generator.GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping, generatedPublishedProviderData[provider.Key]);

                            if (string.IsNullOrWhiteSpace(contents))
                            {
                                throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedProviderVersion.Id}'");
                            }

                            try
                            {
                                await _publishedProviderVersionService.SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, contents);
                                await _publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);
                            }
                            catch (Exception ex)
                            {
                                throw new RetriableException(ex.Message);
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
