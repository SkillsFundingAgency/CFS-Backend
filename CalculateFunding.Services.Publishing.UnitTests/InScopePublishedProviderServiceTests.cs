using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using PublishingProvider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class InScopePublishedProviderServiceTests
    {
        private IMapper _mapper;
        private InScopePublishedProviderService _service;

        private SpecificationSummary _specificationSummary;
        private Dictionary<string, PublishedProvider> _publishedProviders;
        private IEnumerable<ApiProvider> _scopedProviders;
        private Reference _fundingStream;

        private Dictionary<string, PublishedProvider> _results;

        [TestInitialize]
        public void SetUp()
        {
            _mapper = Substitute.For<IMapper>();

            _service = new InScopePublishedProviderService(_mapper,
                Substitute.For<ILogger>());

            _publishedProviders = new Dictionary<string, PublishedProvider>();
        }

        [TestMethod]
        public void ThrowsExceptionIfNoSpecificationSummarySupplied()
        {
            TheExceptionShouldBeThrown("Could not locate a specification id on the supplied specification summary");
        }

        [TestMethod]
        public void ThrowsExceptionIfSpecificationSummarySuppliedHasNoId()
        {
            GivenTheSpecificationSummary(NewSpecificationSummary(_ => _.WithNoId()));

            TheExceptionShouldBeThrown("Could not locate a specification id on the supplied specification summary");
        }

        [TestMethod]
        public void ThrowsExceptionIfSpecificationSummarySuppliedHasNoFundingPeriod()
        {
            GivenTheSpecificationSummary(NewSpecificationSummary(_ => _.WithNoFundingPeriod()));

            TheExceptionShouldBeThrown("Could not locate a funding period id on the supplied specification summary");
        }

        [TestMethod]
        public void ThrowsExceptionIfNoFundingStreamIsSupplied()
        {
            GivenTheSpecificationSummary(NewSpecificationSummary());

            TheExceptionShouldBeThrown("Could not locate a funding stream id from the supplied reference");
        }

        [TestMethod]
        public void ThrowsExceptionIfTheSuppliedFundingStreamHasNoId()
        {
            GivenTheSpecificationSummary(NewSpecificationSummary());
            AndTheFundingStream(NewFundingStream(_ => _.WithNoId()));

            TheExceptionShouldBeThrown("Could not locate a funding stream id from the supplied reference");
        }

        [TestMethod]
        public void CreatesNewPublishedProviderForEachMissingBasedOffTheSuppliedScopedProviders()
        {
            ApiProvider providerOne = NewApiProvider();
            ApiProvider providerTwoMissing = NewApiProvider();
            ApiProvider providerThreeMissing = NewApiProvider();
            ApiProvider providerFour = NewApiProvider();

            GivenTheSpecificationSummary(NewSpecificationSummary());
            AndTheFundingStream(NewFundingStream());
            AndTheScopedProviders(providerOne, providerTwoMissing, providerThreeMissing, providerFour);

            string fundingStreamId = _fundingStream.Id;
            string fundingPeriodId = _specificationSummary.FundingPeriod.Id;
            string specificationId = _specificationSummary.Id;

            AndThePublishedProvider(NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(pv => pv.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithProviderId(providerOne.ProviderId)))));
            AndThePublishedProvider(NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(pv => pv.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithProviderId(providerFour.ProviderId)))));

            PublishingProvider publishingProviderTwo = new PublishingProvider();
            PublishingProvider publishingProviderThree = new PublishingProvider();

            AndTheProviderForApiProvider(providerTwoMissing, publishingProviderTwo);
            AndTheProviderForApiProvider(providerThreeMissing, publishingProviderThree);

            WhenTheMissingProvidersAreGenerated();

            _results
                .Should()
                .NotBeNull();

            AndTheResultsContainsPublishedProviderMatching(NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(pv =>
                pv.WithVersion(1)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithProviderId(providerTwoMissing.ProviderId)
                    .WithProvider(publishingProviderTwo)
                    .WithSpecificationId(specificationId)
                    .WithMajorVersion(0)
                    .WithMinorVersion(1)
                    .WithPublishedProviderStatus(providerTwoMissing.Status.AsEnum<PublishedProviderStatus>())))));
            AndTheResultsContainsPublishedProviderMatching(NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(pv =>
                pv.WithVersion(1)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithProviderId(providerThreeMissing.ProviderId)
                    .WithProvider(publishingProviderThree)
                    .WithSpecificationId(specificationId)
                    .WithMajorVersion(0)
                    .WithMinorVersion(1)
                    .WithPublishedProviderStatus(providerThreeMissing.Status.AsEnum<PublishedProviderStatus>())))));
        }

        private void AndTheResultsContainsPublishedProviderMatching(PublishedProvider expectedPublishedProvider)
        {
            string id = expectedPublishedProvider.Current.ProviderId;

            _results
                .ContainsKey(id)
                .Should()
                .BeTrue();

            _results[id]
                .Should()
                .BeEquivalentTo(expectedPublishedProvider);
        }

        private void TheExceptionShouldBeThrown(string message)
        {
            Action invocation = WhenTheMissingProvidersAreGenerated;

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage(message);
        }

        private void WhenTheMissingProvidersAreGenerated()
        {
            _results = _service.GenerateMissingProviders(_scopedProviders,
                _specificationSummary,
                _fundingStream,
                _publishedProviders,
                new TemplateMetadataContents());
            //not sure this method sig is correct - why do we need the template contents?
        }

        private void GivenTheSpecificationSummary(SpecificationSummary specificationSummary)
        {
            _specificationSummary = specificationSummary;
        }

        private void AndTheFundingStream(Reference fundingStream)
        {
            _fundingStream = fundingStream;
        }

        private void AndTheProviderForApiProvider(ApiProvider apiProvider, PublishingProvider publishingProvider)
        {
            _mapper.Map<PublishingProvider>(apiProvider)
                .Returns(publishingProvider);
        }


        private void AndTheScopedProviders(params ApiProvider[] providers)
        {
            _scopedProviders = providers.ToArray();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private void AndThePublishedProvider(PublishedProvider publishedProvider)
        {
            _publishedProviders.Add(publishedProvider.Current.ProviderId, publishedProvider);
        }

        private Reference NewFundingStream(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private ApiProvider NewApiProvider(Action<ApiProviderBuilder> setUp = null)
        {
            ApiProviderBuilder apiProviderBuilder = new ApiProviderBuilder()
                .WithStatus(PublishedProviderStatus.Draft.ToString());

            setUp?.Invoke(apiProviderBuilder);

            return apiProviderBuilder.Build();
        }
    }
}