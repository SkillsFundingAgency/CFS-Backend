using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class PublishedProviderContentChannelPersistenceServiceTests
    {
        private IPublishedProviderContentChannelPersistenceService _publishedProviderContentChannelPersistenceService;
        private ILogger _logger;
        private IPublishedProviderChannelVersionService _publishedProviderChannelVersionService;
        private IPoliciesService _policiesService;
        private ICalculationsService _calculationsService;
        private IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;

        private const string ProviderVersionProviderId = "providerVersionProviderId";
        private const string ProviderVersionFundingPeriodId = "providerVersionFundingPeriodId";
        private const string ProviderVersionFundingStreamId = "providerVersionFundingStreamId";
        private const string ProviderVersionSpecificationId = "providerVersionSpecificationId";
        private const int ProviderVersionVersion = 1;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _publishedProviderChannelVersionService = Substitute.For<IPublishedProviderChannelVersionService>();
            IConfiguration configuration = Substitute.For<IConfiguration>();
            _policiesService = Substitute.For<IPoliciesService>();
            _calculationsService = Substitute.For<ICalculationsService>();
            _publishedProviderContentsGeneratorResolver = Substitute.For<IPublishedProviderContentsGeneratorResolver>();

            _publishedProviderContentChannelPersistenceService = new PublishedProviderContentChannelPersistenceService(
                _logger,
                new PublishingEngineOptions(configuration),
                _publishedProviderChannelVersionService,
                _policiesService,
                _calculationsService,
                _publishedProviderContentsGeneratorResolver);
        }

        [TestMethod]
        public void ThrowsExceptionWhenPublishedProviderVersionServiceThrowsException()
        {
            // Arrange
            TemplateMapping templateMapping = NewTemplateMapping();
            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            List<PublishedProvider> publishedProvidersToUpdate = new List<PublishedProvider>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion)
                .WithSpecificationId(ProviderVersionSpecificationId));

            List<PublishedProviderVersion> publishedProviderVersions = new List<PublishedProviderVersion> { publishedProviderVersion };

            string channelCode = NewRandomString();
            Channel channel = new Channel { ChannelCode = channelCode };

            string schemaVersionKey
                                = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.TemplateVersion}".ToLower();

            string schemaVersion = NewRandomString();
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithSchemaVersion(schemaVersion));

            _policiesService
                .GetTemplateMetadataContents(publishedProviderVersion.FundingStreamId, publishedProviderVersion.FundingPeriodId, publishedProviderVersion.TemplateVersion)
                .Returns(templateMetadataContents);

            _publishedProviderContentsGeneratorResolver
                .GetService(schemaVersion)
                .Returns(publishedProviderContentsGenerator);

            publishedProviderContentsGenerator
                .GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping)
                .Returns("contents");

            string exceptionMessage = "Exception Message";

            _publishedProviderChannelVersionService
                .SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, Arg.Any<string>(), publishedProviderVersion.SpecificationId, channelCode)
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> invocation = async () => await _publishedProviderContentChannelPersistenceService.SavePublishedProviderContents(
                templateMapping, publishedProviderVersions, channel);

            // Assert
            ThenExceptionShouldBeThrown(exceptionMessage, invocation);
        }

        [TestMethod]
        public async Task SavesPublishedProviderVersionWhenCorrectInputGiven()
        {
            // Arrange
            TemplateMapping templateMapping = NewTemplateMapping();
            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion)
                .WithSpecificationId(ProviderVersionSpecificationId));

            List<PublishedProviderVersion> publishedProviderVersions = new List<PublishedProviderVersion> { publishedProviderVersion };

            string channelCode = NewRandomString();
            Channel channel = new Channel { ChannelCode = channelCode };

            string schemaVersionKey
                    = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.TemplateVersion}".ToLower();

            string schemaVersion = NewRandomString();
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithSchemaVersion(schemaVersion));

            _policiesService
                .GetTemplateMetadataContents(publishedProviderVersion.FundingStreamId, publishedProviderVersion.FundingPeriodId, publishedProviderVersion.TemplateVersion)
                .Returns(templateMetadataContents);

            _publishedProviderContentsGeneratorResolver
                .GetService(schemaVersion)
                .Returns(publishedProviderContentsGenerator);

            publishedProviderContentsGenerator
                .GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping)
                .Returns("contents");

            // Act
            await _publishedProviderContentChannelPersistenceService.SavePublishedProviderContents(
                templateMapping, publishedProviderVersions, channel);

            // Assert
            await _publishedProviderChannelVersionService
                .Received()
                .SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, Arg.Any<string>(), publishedProviderVersion.SpecificationId, channelCode);
        }

        [TestMethod]
        public async Task SavePublishedProviderVariationReasonContentsAndOverridesVariationReasonsWhenCorrectInputGiven()
        {
            // Arrange
            string specificationId = NewRandomString();
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId).WithFundingStreamIds(ProviderVersionFundingStreamId));
            IEnumerable<VariationReason> variationReasons = new List<VariationReason> { VariationReason.AuthorityFieldUpdated, VariationReason.CalculationValuesUpdated };

            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders = new Dictionary<string, IEnumerable<VariationReason>>
            {
                {ProviderVersionProviderId, variationReasons }
            };

            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion)
                .WithSpecificationId(ProviderVersionSpecificationId)
                .WithVariationReasons(new[] { VariationReason.DistrictCodeFieldUpdated }));

            TemplateMapping templateMapping = NewTemplateMapping();

            _calculationsService
                .GetTemplateMapping(specificationId, ProviderVersionFundingStreamId)
                .Returns(templateMapping);

            List<PublishedProviderVersion> publishedProviderVersions = new List<PublishedProviderVersion> { publishedProviderVersion };

            string channelCode = NewRandomString();
            Channel channel = new Channel { ChannelCode = channelCode };

            string schemaVersionKey
                    = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.TemplateVersion}".ToLower();

            string schemaVersion = NewRandomString();
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithSchemaVersion(schemaVersion));

            _policiesService
                .GetTemplateMetadataContents(publishedProviderVersion.FundingStreamId, publishedProviderVersion.FundingPeriodId, publishedProviderVersion.TemplateVersion)
                .Returns(templateMetadataContents);

            _publishedProviderContentsGeneratorResolver
                .GetService(schemaVersion)
                .Returns(publishedProviderContentsGenerator);

            publishedProviderContentsGenerator
                .GenerateContents(
                    Arg.Is<PublishedProviderVersion>(ppv => 
                        ppv.ProviderId == ProviderVersionProviderId &&
                        ppv.VariationReasons == variationReasons), 
                    templateMetadataContents, 
                    templateMapping)
                .Returns("contents");

            // Act
            await _publishedProviderContentChannelPersistenceService.SavePublishedProviderContents(
                specificationSummary, publishedProviderVersions, channel, variationReasonsForProviders);

            // Assert
            await _publishedProviderChannelVersionService
                .Received()
                .SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, Arg.Any<string>(), publishedProviderVersion.SpecificationId, channelCode);
        }

        [TestMethod]
        public async Task SavePublishedProviderVariationReasonContentsAndNullifiesVariationReasonsWhenCorrectInputGiven()
        {
            // Arrange
            string specificationId = NewRandomString();
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId).WithFundingStreamIds(ProviderVersionFundingStreamId));

            IDictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders = new Dictionary<string, IEnumerable<VariationReason>>
            {
            };

            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion)
                .WithSpecificationId(ProviderVersionSpecificationId)
                .WithVariationReasons(new[] { VariationReason.AuthorityFieldUpdated }));

            TemplateMapping templateMapping = NewTemplateMapping();

            _calculationsService
                .GetTemplateMapping(specificationId, ProviderVersionFundingStreamId)
                .Returns(templateMapping);

            List<PublishedProviderVersion> publishedProviderVersions = new List<PublishedProviderVersion> { publishedProviderVersion };

            string channelCode = NewRandomString();
            Channel channel = new Channel { ChannelCode = channelCode };

            string schemaVersionKey
                    = $"{publishedProviderVersion.FundingStreamId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.TemplateVersion}".ToLower();

            string schemaVersion = NewRandomString();
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithSchemaVersion(schemaVersion));

            _policiesService
                .GetTemplateMetadataContents(publishedProviderVersion.FundingStreamId, publishedProviderVersion.FundingPeriodId, publishedProviderVersion.TemplateVersion)
                .Returns(templateMetadataContents);

            _publishedProviderContentsGeneratorResolver
                .GetService(schemaVersion)
                .Returns(publishedProviderContentsGenerator);

            publishedProviderContentsGenerator
                .GenerateContents(
                    Arg.Is<PublishedProviderVersion>(ppv =>
                        ppv.ProviderId == ProviderVersionProviderId &&
                        ppv.VariationReasons == Array.Empty<VariationReason>()),
                    templateMetadataContents,
                    templateMapping)
                .Returns("contents");

            // Act
            await _publishedProviderContentChannelPersistenceService.SavePublishedProviderContents(
                specificationSummary, publishedProviderVersions, channel, variationReasonsForProviders);

            // Assert
            await _publishedProviderChannelVersionService
                .Received()
                .SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, Arg.Any<string>(), publishedProviderVersion.SpecificationId, channelCode);
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        private TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateMetadataContentsBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(templateMetadataContentsBuilder);

            return templateMetadataContentsBuilder.Build();
        }

        private TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();

            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private void ThenExceptionShouldBeThrown(string message, Func<Task> invocation)
        {
            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage(message);
        }

        private string NewRandomString() => new RandomString();
    }
}
