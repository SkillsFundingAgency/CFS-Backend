using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class DistributionProfileStrategyTests : VariationStrategyTestBase
    {
        private DistributionProfileStrategy _variationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _variationStrategy = new DistributionProfileStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfNoReleasedState()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.PublishedProvider.Released = null);

            VariationStrategyResult result = await WhenTheVariationsAreDetermined();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();

            result
                .StopSubsequentStrategies
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderClosed()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[]
            {
                NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Information)),
                NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Contracting))
            };

            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData
                = new Dictionary<string, IEnumerable<OrganisationGroupResult>> { { $"{fundingStreamId}:{fundingPeriodId}", organisationGroupResults } };

            GivenTheOtherwiseValidVariationContext(_ => 
            {
                _.PublishedProvider = NewPublishedProvider(p => p.WithReleased(
                                    NewPublishedProviderVersion(ppv => 
                                        ppv.WithFundingStreamId(fundingStreamId)
                                        .WithFundingPeriodId(fundingPeriodId))));
                _.OrganisationGroupResultsData = organisationGroupResultsData;
            });

            VariationStrategyResult result = await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<SetProfilePeriodValuesChange>();
            VariationContext
                .VariationReasons
                .Should()
                .Contain(VariationReason.DistributionProfileUpdated);

            result
                .StopSubsequentStrategies
                .Should()
                .BeTrue();
        }

        private async Task<VariationStrategyResult> WhenTheVariationsAreDetermined()
        {
            return await _variationStrategy.DetermineVariations(VariationContext, null);
        }

        private static OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        //private static OrganisationIdentifier NewOrganisationIdentifier(Action<OrganisationIdentifierBuilder> setUp = null)
        //{
        //    OrganisationIdentifierBuilder organisationIdentifierBuilder = new OrganisationIdentifierBuilder();

        //    setUp?.Invoke(organisationIdentifierBuilder);

        //    return organisationIdentifierBuilder.Build();
        //}

        //protected static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        //{
        //    PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

        //    setUp?.Invoke(publishedProviderBuilder);

        //    return publishedProviderBuilder.Build();
        //}

        //protected static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        //{
        //    PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder()
        //        .WithProvider(NewProvider());

        //    setUp?.Invoke(providerVersionBuilder);

        //    return providerVersionBuilder.Build();
        //}

        //private static string NewRandomString() => new RandomString();
    }
}
