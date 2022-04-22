using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class NoApplicableProfilingUpdateVariationErrorDetectorTests : PublishedProviderErrorDetectorTest
    {
        private NoApplicableProfilingUpdateVariationErrorDetector _errorDetector;
        private Mock<ILogger> _logger;
        private Mock<IPoliciesService> _policiesService;
        private const string CLOSURE_WITH_SUCCESSOR_STRATEGY = "ClosureWithSuccessor";

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new NoApplicableProfilingUpdateVariationErrorDetector();
            _logger = new Mock<ILogger>();
            _policiesService = new Mock<IPoliciesService>();
        }

        [TestMethod]
        public void RunsForPreVariations()
        {
            _errorDetector.IsPostVariationCheck
                .Should()
                .BeTrue();

            _errorDetector.IsPreVariationCheck
                .Should()
                .BeFalse();

            _errorDetector.IsForAllFundingConfigurations
                .Should()
                .BeFalse();

            _errorDetector.IsAssignProfilePatternCheck
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task NoErrorsWhenPublishedProviderNotReleased()
        {
            string providerId = NewRandomString();
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerId))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, 
                new PublishedProvidersContext { 
                    VariationContexts = new Dictionary<string, ProviderVariationContext>(
                            new[] { 
                                new KeyValuePair<string, ProviderVariationContext>(providerId, new ProviderVariationContext(_policiesService.Object, _logger.Object)) 
                            }
                        ) 
                });

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        [TestMethod]
        public async Task WhenASuccessorsFundingChangesAndThePredecessorHasApplicableVariationNoErrorsDetected()
        {
            string successorProviderId = NewRandomString();
            string predecessorProviderId = NewRandomString();
            string fundingLineCode = NewRandomString();
            PublishedProvider publishedProvider = NewPublishedProvider(_ => 
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProviderId(successorProviderId)
                    .WithPredecessors(predecessorProviderId)
                    .WithFundingLines(new FundingLine { FundingLineCode = fundingLineCode, Value = 1000 })))
                .WithReleased(NewPublishedProviderVersion(ppv => ppv.WithProviderId(successorProviderId))));

            ProviderVariationContext successorProviderVariationContext = new ProviderVariationContext(_policiesService.Object, _logger.Object);

            successorProviderVariationContext.VariationPointers = new ProfileVariationPointer[] { new ProfileVariationPointer { FundingLineId = fundingLineCode } };
            successorProviderVariationContext.PublishedProvider = publishedProvider;

            ProviderVariationContext predecessorProviderVariationContext = new ProviderVariationContext(_policiesService.Object, _logger.Object);

            predecessorProviderVariationContext.AddAffectedFundingLineCode(CLOSURE_WITH_SUCCESSOR_STRATEGY, fundingLineCode);

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider,
                new PublishedProvidersContext
                {
                    VariationContexts = new Dictionary<string, ProviderVariationContext>(
                            new[] {
                                new KeyValuePair<string, ProviderVariationContext>(successorProviderId, successorProviderVariationContext),
                                new KeyValuePair<string, ProviderVariationContext>(predecessorProviderId, predecessorProviderVariationContext)
                            }
                        )
                });

            publishedProvider.Current.HasErrors
                .Should()
                .BeFalse();
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
        }
    }
}
