﻿using CalculateFunding.Generators.OrganisationGroup.Enums;
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

            bool result = await WhenTheVariationsAreDetermined();

            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task StopsSubsequentStrategiesIfPreconditionsMet()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(p => p.WithReleased(
                                    NewPublishedProviderVersion(ppv =>
                                        ppv.WithFundingStreamId(fundingStreamId)
                                        .WithFundingPeriodId(fundingPeriodId))));

            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[]
            {
                NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Information)),
                NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Contracting)
                    .WithProviders(new[] {new Common.ApiClient.Providers.Models.Provider{ ProviderId = publishedProvider.Current.ProviderId} }))
            };

            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData
                = new Dictionary<string, IEnumerable<OrganisationGroupResult>> { { $"{fundingStreamId}:{fundingPeriodId}", organisationGroupResults } };

            
            GivenTheOtherwiseValidVariationContext(_ => 
            {
                _.PublishedProvider = publishedProvider;
                _.OrganisationGroupResultsData = organisationGroupResultsData;
            });

            bool result = await WhenTheVariationsAreDetermined();

            result
                .Should()
                .BeTrue();
        }

        private async Task<bool> WhenTheVariationsAreDetermined()
        {
            return await _variationStrategy.Process(VariationContext, null);
        }

        private static OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }
    }
}
