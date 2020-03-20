using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.UnitTests.Reporting.FundingLines
{
    public abstract class FundingLineCsvTransformTestBase
    {
        protected static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected static IEnumerable<Provider> NewProviders(params Action<ProviderBuilder>[] setUps)
        {
            return setUps.Select(NewProvider);
        }

        protected static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        protected static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        protected static IEnumerable<FundingLine> NewFundingLines(params Action<FundingLineBuilder>[] setUps)
        {
            return setUps.Select(NewFundingLine);
        }

        protected static Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        protected static IEnumerable<PublishedProvider> NewPublishedProviders(params Action<PublishedProviderBuilder>[] setUps)
        {
            return setUps.Select(NewPublishedProvider);
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static PublishedFundingOrganisationGrouping NewPublishedFundingOrganisationGrouping(Action<PublishedFundingOrganisationGroupingBuilder> setUp = null)
        {
            PublishedFundingOrganisationGroupingBuilder publishedFundingOrganisationGroupingBuilder = new PublishedFundingOrganisationGroupingBuilder();

            setUp?.Invoke(publishedFundingOrganisationGroupingBuilder);

            return publishedFundingOrganisationGroupingBuilder.Build();
        }

        protected static IEnumerable<PublishedFundingOrganisationGrouping> NewPublishedFundingOrganisationGroupings(params Action<PublishedFundingOrganisationGroupingBuilder>[] setUps)
        {
            return setUps.Select(NewPublishedFundingOrganisationGrouping);
        }

        protected static PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        protected static PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }

        protected static IEnumerable<PublishedFundingVersion> NewPublishedFundingVersions(params Action<PublishedFundingVersionBuilder>[] setUps)
        {
            return setUps.Select(NewPublishedFundingVersion);
        }

        protected static OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }
    }
}