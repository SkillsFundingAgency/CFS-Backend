using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class FundingSchemaUpdatedVariationStrategyTests : VariationStrategyTestBase
    {
        private FundingSchemaUpdatedVariationStrategy _fundingSchemaUpdatedVariationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _fundingSchemaUpdatedVariationStrategy = new FundingSchemaUpdatedVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfNoPriorStateYet()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
                _.AllPublishedProvidersRefreshStates = new Dictionary<string, PublishedProvider>();
            });

            await WhenTheVariationsAreDetermined();

            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfThereIsNoPreviouslyReleasedVersion()
        {
            GivenTheOtherwiseValidVariationContext();
            AndThereIsNoPreviouslyReleasedVersion();

            await WhenTheVariationsAreDetermined();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task SetTheVariationReasonIfFundingSchemaVersionIsUpated()
        {
            string priorTemplateVersion = NewRandomString();
            string updatedTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string priorSchemeVersion = NewRandomString();
            string updatedSchemVersion = NewRandomString();

            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.PriorState.FundingStreamId = fundingStreamId;
                _.PriorState.FundingPeriodId = fundingPeriodId;
                _.PriorState.TemplateVersion = priorTemplateVersion;
                _.ReleasedState.FundingStreamId = fundingStreamId;
                _.ReleasedState.FundingPeriodId = fundingPeriodId;
                _.ReleasedState.TemplateVersion = updatedTemplateVersion;
            });
            AndSchemaVersion(fundingStreamId, fundingPeriodId, priorTemplateVersion, priorSchemeVersion);
            AndSchemaVersion(fundingStreamId, fundingPeriodId, updatedTemplateVersion, updatedSchemVersion);

            await WhenTheVariationsAreDetermined();

            VariationContext
                .VariationReasons
                .Should()
                .BeEquivalentTo(new[] { VariationReason.FundingSchemaUpdated });

            VariationContext
               .QueuedChanges
               .Should()
               .NotBeEmpty();

            VariationContext
              .QueuedChanges
              .First()
              .Should()
              .BeOfType<MetaDataVariationsChange>();
        }

        private void AndSchemaVersion(string fundingStreamId, string fundingPeriodId, string templateVersion, string schemeVersion)
        {
            _policiesService.GetTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion)
                .Returns(new Common.TemplateMetadata.Models.TemplateMetadataContents { SchemaVersion = schemeVersion });
        }

        private async Task WhenTheVariationsAreDetermined()
        {
            await _fundingSchemaUpdatedVariationStrategy.DetermineVariations(VariationContext, null);
        }

        private void AndThereIsNoPreviouslyReleasedVersion()
        {
            VariationContext.PublishedProvider.Released = null;
        }
    }
}
