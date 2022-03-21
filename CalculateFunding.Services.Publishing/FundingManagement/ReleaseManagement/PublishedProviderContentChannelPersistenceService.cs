using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class PublishedProviderContentChannelPersistenceService : IPublishedProviderContentChannelPersistenceService
    {
        private readonly ILogger _logger;
        private readonly IPublishingEngineOptions _publishingEngineOptions;
        private readonly IPublishedProviderChannelVersionService _publishedProviderChannelVersionService;
        private readonly IPoliciesService _policiesService;
        private readonly ICalculationsService _calculationsService;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;

        public PublishedProviderContentChannelPersistenceService(
            ILogger logger,
            IPublishingEngineOptions publishingEngineOptions,
            IPublishedProviderChannelVersionService publishedProviderChannelVersionService,
            IPoliciesService policiesService,
            ICalculationsService calculationsService,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(publishedProviderChannelVersionService, nameof(publishedProviderChannelVersionService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(calculationsService, nameof(calculationsService));
            Guard.ArgumentNotNull(publishedProviderContentsGeneratorResolver, nameof(publishedProviderContentsGeneratorResolver));

            _logger = logger;
            _publishingEngineOptions = publishingEngineOptions;
            _publishedProviderChannelVersionService = publishedProviderChannelVersionService;
            _calculationsService = calculationsService;
            _policiesService = policiesService;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
        }

        public async Task SavePublishedProviderContents(
            SpecificationSummary specification,
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            Channel channel,
            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders)
        {
            OverrideVariationReasons(publishedProviderVersions, variationReasonsForProviders);

            TemplateMapping templateMapping = await GetTemplateMapping(specification.FundingStreams.FirstOrDefault(), specification.Id);

            await SavePublishedProviderContents(
                templateMapping,
                publishedProviderVersions,
                channel);
        }

        public async Task SavePublishedProviderContents(
            TemplateMapping templateMapping,
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            Channel channel)
        {
            _logger.Information("Saving published provider contents");
            Dictionary<string, TemplateMetadataContents> schemaVersions = new Dictionary<string, TemplateMetadataContents>();

            IEnumerable<string> templateVersions = publishedProviderVersions.GroupBy(_ => _.TemplateVersion).Select(_ => _.Key);

            foreach (string templateVersion in templateVersions)
            {
                if (string.IsNullOrWhiteSpace(templateVersion))
                {
                    throw new InvalidOperationException("Template version is null or empty string");
                }

                TemplateMetadataContents templateContents =
                                    await _policiesService.GetTemplateMetadataContents(
                                            publishedProviderVersions.First().FundingStreamId,
                                            publishedProviderVersions.First().FundingPeriodId,
                                            templateVersion);

                schemaVersions.Add(templateVersion, templateContents);
            }


            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.SavePublishedProviderContentsConcurrencyCount);
            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string schemaVersionKey
                                = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.TemplateVersion}".ToLower();

                            TemplateMetadataContents templateContents = schemaVersions[publishedProviderVersion.TemplateVersion];

                            IPublishedProviderContentsGenerator generator = _publishedProviderContentsGeneratorResolver.GetService(templateContents.SchemaVersion);

                            string contents = generator.GenerateContents(publishedProviderVersion, templateContents, templateMapping);


                            if (string.IsNullOrWhiteSpace(contents))
                            {
                                throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedProviderVersion.Id}'");
                            }

                            try
                            {
                                await _publishedProviderChannelVersionService.SavePublishedProviderVersionBody(
                                    publishedProviderVersion.FundingId,
                                    contents,
                                    publishedProviderVersion.SpecificationId,
                                    channel.ChannelCode);
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

        private void OverrideVariationReasons(
            IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders)
        {
            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
            {
                if (variationReasonsForProviders.ContainsKey(publishedProviderVersion.ProviderId))
                {
                    publishedProviderVersion.VariationReasons = variationReasonsForProviders[publishedProviderVersion.ProviderId];
                }
                else
                {
                    publishedProviderVersion.VariationReasons = Array.Empty<VariationReason>();
                }
            }
        }

        private async Task<TemplateMapping> GetTemplateMapping(Reference fundingStream, string specificationId)
        {
            TemplateMapping calculationMapping = await _calculationsService.GetTemplateMapping(specificationId, fundingStream.Id);

            if (calculationMapping == null)
            {
                throw new Exception($"CalculationMapping returned null for specification {specificationId} and funding stream {fundingStream.Id}");
            }

            return calculationMapping;
        }
    }
}
