using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NSubstitute.ExceptionExtensions;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderContentPersistanceServiceTests
    {
        private IPublishedProviderContentPersistanceService _publishedProviderContentPersistanceService;

        private ILogger _logger;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private IPublishedProviderVersioningService _publishedProviderVersioningService;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;

        private const string key = "providerVersionProviderId";
        private const string ProviderVersionProviderId = "providerVersionProviderId";
        private const string ProviderVersionFundingPeriodId = "providerVersionFundingPeriodId";
        private const string ProviderVersionFundingStreamId = "providerVersionFundingStreamId";
        private const string ProviderVersionSpecificationId = "providerVersionSpecificationId";
        private const int ProviderVersionVersion = 1;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();
            _publishedProviderVersioningService = Substitute.For<IPublishedProviderVersioningService>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            IConfiguration configuration = Substitute.For<IConfiguration>();

            _publishedProviderContentPersistanceService = new PublishedProviderContentPersistanceService(
                _publishedProviderVersionService,
                _publishedProviderVersioningService,
                _publishedProviderIndexerService,
                _logger,
                new PublishingEngineOptions(configuration));
        }

        [TestMethod]
        public void ThrowsExceptionWhenContentGeneratorReturnsNull()
        {
            // Arrange
            TemplateMetadataContents templateMetadataContents = Substitute.For<TemplateMetadataContents>();
            TemplateMapping templateMapping = Substitute.For<TemplateMapping>();
            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            Dictionary<string, GeneratedProviderResult> generatedPublishedProviderData = new Dictionary<string, GeneratedProviderResult>();
            List<PublishedProvider> publishedProvidersToUpdate = new List<PublishedProvider>();

            GeneratedProviderResult generatedProviderResult = new GeneratedProviderResult();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion));

            PublishedProvider publishedProvider = NewPublishedProvider(provider => provider.WithCurrent(publishedProviderVersion));

            generatedPublishedProviderData.Add(key, generatedProviderResult);
            publishedProvidersToUpdate.Add(publishedProvider);

            // Act
            Func<Task> invocation = async () => await _publishedProviderContentPersistanceService.SavePublishedProviderContents(
                templateMetadataContents, templateMapping, publishedProvidersToUpdate, publishedProviderContentsGenerator);

            // Assert
            ThenExceptionShouldBeThrown($"Generator failed to generate content for published provider version with id: '{publishedProviderVersion.Id}'", invocation);
        }

        [TestMethod]
        public void ThrowsExceptionWhenPublishedProviderVersionServiceThrowsException()
        {
            // Arrange
            TemplateMetadataContents templateMetadataContents = Substitute.For<TemplateMetadataContents>();
            TemplateMapping templateMapping = Substitute.For<TemplateMapping>();
            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            List<PublishedProvider> publishedProvidersToUpdate = new List<PublishedProvider>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion)
                .WithSpecificationId(ProviderVersionSpecificationId));

            PublishedProvider publishedProvider = NewPublishedProvider(provider => provider.WithCurrent(publishedProviderVersion));

            publishedProvidersToUpdate.Add(publishedProvider);

            publishedProviderContentsGenerator
                .GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping)
                .Returns("contents");

            string exceptionMessage = "Exception Message";

            _publishedProviderVersionService
                .SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, Arg.Any<string>(), publishedProviderVersion.SpecificationId)
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> invocation = async () => await _publishedProviderContentPersistanceService.SavePublishedProviderContents(
                templateMetadataContents, templateMapping, publishedProvidersToUpdate, publishedProviderContentsGenerator);

            // Assert
            ThenExceptionShouldBeThrown(exceptionMessage, invocation);
        }

        [TestMethod]
        public void ThrowsExceptionWhenPublishedProviderIndexerServiceThrowsException()
        {
            // Arrange
            TemplateMetadataContents templateMetadataContents = Substitute.For<TemplateMetadataContents>();
            TemplateMapping templateMapping = Substitute.For<TemplateMapping>();
            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            List<PublishedProvider> publishedProvidersToUpdate = new List<PublishedProvider>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion));

            PublishedProvider publishedProvider = NewPublishedProvider(provider => provider.WithCurrent(publishedProviderVersion));

            publishedProvidersToUpdate.Add(publishedProvider);

            publishedProviderContentsGenerator
                .GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping)
                .Returns("contents");

            string exceptionMessage = "Exception Message";

            _publishedProviderIndexerService
                .IndexPublishedProvider(publishedProviderVersion)
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> invocation = async () => await _publishedProviderContentPersistanceService.SavePublishedProviderContents(
                templateMetadataContents, templateMapping, publishedProvidersToUpdate, publishedProviderContentsGenerator);

            // Assert
            ThenExceptionShouldBeThrown(exceptionMessage, invocation);
        }

        [TestMethod]
        public async Task SavesAndIndexesPublishedProviderVersionWhenCorrectInputGiven()
        {
            // Arrange
            TemplateMetadataContents templateMetadataContents = Substitute.For<TemplateMetadataContents>();
            TemplateMapping templateMapping = Substitute.For<TemplateMapping>();
            IPublishedProviderContentsGenerator publishedProviderContentsGenerator = Substitute.For<IPublishedProviderContentsGenerator>();

            List<PublishedProvider> publishedProvidersToUpdate = new List<PublishedProvider>();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(providerVersion => providerVersion
                .WithProviderId(ProviderVersionProviderId)
                .WithFundingPeriodId(ProviderVersionFundingPeriodId)
                .WithFundingStreamId(ProviderVersionFundingStreamId)
                .WithVersion(ProviderVersionVersion)
                .WithSpecificationId(ProviderVersionSpecificationId));

            PublishedProvider publishedProvider = NewPublishedProvider(provider => provider.WithCurrent(publishedProviderVersion));

            publishedProvidersToUpdate.Add(publishedProvider);

            publishedProviderContentsGenerator
                .GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping)
                .Returns("contents");

            // Act
            await _publishedProviderContentPersistanceService.SavePublishedProviderContents(
                templateMetadataContents, templateMapping, publishedProvidersToUpdate, publishedProviderContentsGenerator);

            // Assert
            await _publishedProviderVersionService
                .Received()
                .SavePublishedProviderVersionBody(publishedProviderVersion.FundingId, Arg.Any<string>(), publishedProviderVersion.SpecificationId);

            await _publishedProviderIndexerService
                .Received()
                .IndexPublishedProvider(publishedProviderVersion);
        }

        private void ThenExceptionShouldBeThrown(string message, Func<Task> invocation)
        {
            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage(message);
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder providerBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }
    }
}
