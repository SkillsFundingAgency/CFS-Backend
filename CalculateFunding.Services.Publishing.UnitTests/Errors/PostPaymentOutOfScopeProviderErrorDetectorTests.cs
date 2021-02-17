using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class PostPaymentOutOfScopeProviderErrorDetectorTests
    {
        private PostPaymentOutOfScopeProviderErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new PostPaymentOutOfScopeProviderErrorDetector();
        }

        [TestMethod]
        public async Task NoErrorsWhenPublishedProviderNotReleased()
        {
            string providerId = NewRandomString();
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerId))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, new PublishedProvidersContext());

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        [TestMethod]
        public async Task ErrorsWhenPublishedProviderHasReleased()
        {
            string providerId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _
                .WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerId).WithFundingStreamId(fundingStreamId)))
                .WithReleased(NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerId).WithFundingStreamId(fundingStreamId))));

            PublishedProvidersContext publishedProvidersContext = new PublishedProvidersContext
            {
                ScopedProviders = new List<Provider>(),
                SpecificationId = specificationId
            };

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, publishedProvidersContext);

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithType(PublishedProviderErrorType.PostPaymentOutOfScopeProvider)
                    .WithSummaryErrorMessage("Post Payment - Provider is not in scope of specification")
                    .WithDetailedErrorMessage($"Provider {providerId} does not exists on in scope providers of specification {specificationId}")
                    .WithFundingLineCode(string.Empty)
                    .WithFundingLine(string.Empty)
                    .WithFundingStreamId(fundingStreamId)));
        }

        private void AndPublishedProviderShouldHaveTheErrors(PublishedProviderVersion providerVersion,
            params PublishedProviderError[] expectedErrors)
        {
            providerVersion.Errors
                .Count
                .Should()
                .Be(expectedErrors.Length);

            foreach (PublishedProviderError expectedError in expectedErrors)
            {
                PublishedProviderError actualError = providerVersion.Errors
                    .SingleOrDefault(_ => _.Identifier == expectedError.Identifier);

                actualError
                    .Should()
                    .BeEquivalentTo(expectedError);
            }
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static PublishedProviderError NewError(Action<PublishedProviderErrorBuilder> setUp = null)
        {
            PublishedProviderErrorBuilder providerErrorBuilder = new PublishedProviderErrorBuilder();

            setUp?.Invoke(providerErrorBuilder);

            return providerErrorBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
