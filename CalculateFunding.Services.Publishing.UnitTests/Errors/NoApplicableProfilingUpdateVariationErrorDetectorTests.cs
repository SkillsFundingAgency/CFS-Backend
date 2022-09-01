using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
                new PublishedProvidersContext
                {
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
        public async Task ErrorsIfNotRunAndVariationPointerSetWithinDisributionPeriods()
        {
            var publishedProvider = await CheckDistibutionPeriods(2);

            publishedProvider.Current.HasErrors
                .Should()
                .BeTrue();

            publishedProvider.Current.Errors.First().DetailedErrorMessage
                .Should()
                .BeEquivalentTo($"Post Profiling and Variations - No applicable variation strategy executed for profiling update from £9 to £5 against funding line {publishedProvider.Current.Errors.First().FundingLineCode}.");
        }

        [TestMethod]
        public async Task NoErrorsIfNotRunAndVariationPointerSetBeforeDisributionPeriods()
        {
            var publishedProvider = await CheckDistibutionPeriods(1);

            publishedProvider.Current.HasErrors
                .Should()
                .BeFalse();
        }

        private async Task<PublishedProvider> CheckDistibutionPeriods(int variationPointerOccurrence)
        {
            string providerId = NewRandomString();
            string fundingLineId = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _
                                                    .WithCurrent(NewPublishedProviderVersion(ppv => ppv
                                                                .WithProviderId(providerId)
                                                                .WithFundingLines(new FundingLine
                                                                {
                                                                    FundingLineCode = fundingLineId,
                                                                    TemplateLineId = 1,
                                                                    Type = FundingLineType.Payment,
                                                                    Value = 5M,
                                                                    DistributionPeriods = new DistributionPeriod[]
                                                                        {
                                                                            new DistributionPeriod
                                                                            {
                                                                                Value = 5M,
                                                                                ProfilePeriods = new ProfilePeriod[]
                                                                                {
                                                                                    new ProfilePeriod
                                                                                    {
                                                                                        Type = ProfilePeriodType.CalendarMonth,
                                                                                        TypeValue = "October",
                                                                                        Occurrence = 1,
                                                                                        Year = DateTime.Now.Year,
                                                                                        ProfiledValue = 5M
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                })))
                                                    .WithReleased(NewPublishedProviderVersion(ppv => ppv
                                                                .WithProviderId(providerId)
                                                                .WithFundingLines(new FundingLine
                                                                {
                                                                    FundingLineCode = fundingLineId,
                                                                    Type = FundingLineType.Payment,
                                                                    Value = 9M,
                                                                    DistributionPeriods = new DistributionPeriod[]
                                                                        {
                                                                            new DistributionPeriod
                                                                            {
                                                                                Value = 9M,
                                                                                ProfilePeriods = new ProfilePeriod[]
                                                                                {
                                                                                    new ProfilePeriod
                                                                                    {
                                                                                        Type = ProfilePeriodType.CalendarMonth,
                                                                                        TypeValue = "October",
                                                                                        Occurrence = 1,
                                                                                        Year = DateTime.Now.Year,
                                                                                        ProfiledValue = 9M
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                }))));

            ProviderVariationContext providerVariationContext = new ProviderVariationContext(_policiesService.Object, _logger.Object);
            providerVariationContext.VariationPointers = new ProfileVariationPointer[]
            {
                new ProfileVariationPointer
                {
                    FundingLineId = fundingLineId,
                    TypeValue = "October",
                    Year = DateTime.Now.Year,
                    Occurrence = variationPointerOccurrence
                }
            };
            providerVariationContext.PublishedProvider = publishedProvider;
            providerVariationContext.PreRefreshState = publishedProvider.Released;

            providerVariationContext.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>()
                {
                    { publishedProvider.Current.ProviderId, new PublishedProviderSnapShots(publishedProvider) }
                };

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider,
                new PublishedProvidersContext
                {
                    VariationContexts = new Dictionary<string, ProviderVariationContext>(
                            new[] {
                                new KeyValuePair<string, ProviderVariationContext>(providerId, providerVariationContext)
                            }
                        )
                });

            return publishedProvider;
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
