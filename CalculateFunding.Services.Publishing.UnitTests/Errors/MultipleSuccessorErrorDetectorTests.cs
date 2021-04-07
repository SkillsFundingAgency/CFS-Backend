using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class MultipleSuccessorErrorDetectorTests : PublishedProviderErrorDetectorTest
    {
        private MultipleSuccessorErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new MultipleSuccessorErrorDetector();
        }

        [TestMethod]
        public void RunsForPreVariations()
        {
            _errorDetector.IsPostVariationCheck
                .Should()
                .BeFalse();

            _errorDetector.IsPreVariationCheck
                .Should()
                .BeTrue();

            _errorDetector.IsForAllFundingConfigurations
                .Should()
                .BeFalse();

            _errorDetector.IsAssignProfilePatternCheck
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task DetectsErrorIfProviderHasMultipleSuccessors()
        {
            string fundingStreamId = NewRandomString();
            
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                    ppv.WithFundingStreamId(fundingStreamId)
                        .WithProvider(NewProvider(p =>
                            p.WithSuccessors(NewRandomString(), NewRandomString()))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            IEnumerable<string> successors = publishedProvider.Current.Provider.GetSuccessors();
            
            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithoutFundingLineCode()
                    .WithIdentifier(publishedProvider.Current.ProviderId)
                    .WithType(PublishedProviderErrorType.MultipleSuccessors)
                    .WithSummaryErrorMessage("The published provider has multiple successors.")
                    .WithDetailedErrorMessage($"Published provider {publishedProvider.Current.ProviderId} has the following successors {successors.JoinWith(',')}")
                    .WithFundingStreamId(fundingStreamId)));
        }
        
        [TestMethod]
        public async Task DetectsNoErrorIfProviderHasSingleOrNoSuccessors()
        {
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv => 
                    ppv.WithProvider(NewProvider()))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);
            
            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider)
        {
            await _errorDetector.DetectErrors(publishedProvider, null);
        }

        private Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }
    }
}