using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.FundingManagement
{
    [TestClass]
    public class ReleaseApprovedProvidersServiceTests
    {
        private IReleaseApprovedProvidersService _releaseApprovedProvidersService;
        private Mock<IPublishService> _publishService;
        private Mock<IPublishedProvidersLoadContext> _publishedProvidersLoadContext;
        private Mock<IReleaseToChannelSqlMappingContext> _releaseContext;
        private SpecificationSummary _specification;
        private string _correlationId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _jobId;
        private Reference _author;

        [TestInitialize]
        public void SetUp()
        {
            _publishService = new Mock<IPublishService>();
            _publishedProvidersLoadContext = new Mock<IPublishedProvidersLoadContext>();
            _releaseContext = new Mock<IReleaseToChannelSqlMappingContext>();

            _specification = NewSpecificationSummary();
            _correlationId = NewRandomString();
            _fundingStreamId = "FS1";
            _fundingPeriodId = "FP2";
            _jobId = NewRandomString();
            _author = NewReference();

            _releaseContext.SetupGet(_ => _.Author)
                .Returns(_author);

            _releaseContext.SetupGet(_ => _.CorrelationId)
                .Returns(_correlationId);

            _releaseContext.SetupGet(_ => _.JobId)
               .Returns(_jobId);

            _releaseApprovedProvidersService = new ReleaseApprovedProvidersService(
                _publishService.Object,
                _publishedProvidersLoadContext.Object,
                _releaseContext.Object
            );
        }

        [TestMethod]
        [DynamicData(nameof(ExampleProvidersAndExpectedResult), DynamicDataSourceType.Method)]
        public async Task ReleaseApprovedProvidersService_OnlyReleasesProvidersInApprovedState(IEnumerable<PublishedProvider> publishedProviders,
            IEnumerable<string> expectedPublishedProviderIds,
            IEnumerable<string> expectedProviderIdsFromReturn)
        {
            GivenPublishedProvidersContext(publishedProviders);
            IEnumerable<string> providerIds = await WhenReleaseProvidersInApprovedState();
            ThenProvidersReleased(expectedPublishedProviderIds);

            providerIds
                .Should()
                .BeEquivalentTo(expectedProviderIdsFromReturn);
        }

        private void GivenPublishedProvidersContext(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProvidersLoadContext.Setup(_ => _.Values).Returns(publishedProviders);
        }

        private async Task<IEnumerable<string>> WhenReleaseProvidersInApprovedState()
        {
            return await _releaseApprovedProvidersService.ReleaseProvidersInApprovedState(
                _specification);
        }

        private void ThenProvidersReleased(IEnumerable<string> publishedProviderIds)
        {
            if (publishedProviderIds.Any())
            {
                _publishService.Verify(_ => _.PublishProviderFundingResults(true,
                    _author,
                    _jobId,
                    _correlationId,
                    _specification,
                    It.Is<PublishedProviderIdsRequest>(ppr =>
                        ppr.PublishedProviderIds.SequenceEqual(publishedProviderIds)), false)
                );
            }
        }

        private static IEnumerable<object[]> ExampleProvidersAndExpectedResult()
        {
            PublishedProvider[] publishedProviders = ArraySegment<PublishedProvider>.Empty.ToArray();

            yield return new object[] { publishedProviders, ArraySegment<string>.Empty.ToArray(), ArraySegment<string>.Empty.ToArray() };

            publishedProviders = GetPublishedProviders(3, PublishedProviderStatus.Approved, publishedProviders);

            yield return new object[] { publishedProviders, publishedProviders.Select(_ => $"{_.Current.FundingStreamId}-{_.Current.FundingPeriodId}-{_.Current.ProviderId}"), publishedProviders.Where(_ => _.Current.Status == PublishedProviderStatus.Approved).Select(_ => _.Current.ProviderId) };

            publishedProviders = GetPublishedProviders(3, PublishedProviderStatus.Updated, publishedProviders);
            publishedProviders = GetPublishedProviders(3, PublishedProviderStatus.Draft, publishedProviders);
            publishedProviders = GetPublishedProviders(3, PublishedProviderStatus.Released, publishedProviders);

            yield return new object[] { publishedProviders, publishedProviders.Where(_ => _.Current.Status == PublishedProviderStatus.Approved).Select(_ => $"{_.Current.FundingStreamId}-{_.Current.FundingPeriodId}-{_.Current.ProviderId}"), publishedProviders.Where(_ => _.Current.Status == PublishedProviderStatus.Approved).Select(_ => _.Current.ProviderId) };
        }

        private static PublishedProvider[] GetPublishedProviders(int count, PublishedProviderStatus status,
                PublishedProvider[] publishedProviders)
        {
            for (int i = 0; i <= count; i++)
            {
                publishedProviders = publishedProviders.Concat(new[] {
                    NewPublishedProvider(_ =>
                    _.WithCurrent(NewPublishedProviderVersion(ppv =>
                        ppv.WithPublishedProviderStatus(status)
                        .WithFundingStreamId("FS1")
                        .WithFundingPeriodId("FP2")
                    )))
                }).ToArray();
            }

            return publishedProviders;
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setup = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setup?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setup = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setup?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setup = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setup?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private static Reference NewReference(Action<ReferenceBuilder> setup = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setup?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private static string NewRandomString()
        {
            return new RandomString();
        }
    }
}
