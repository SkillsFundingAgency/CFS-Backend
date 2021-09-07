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

            result
                .StopSubsequentStrategies
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task StopsSubsequentStrategiesIfPreconditionsMet()
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
    }
}
