using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ProviderMetadataVariationStrategyTests
    {
        private ProviderMetadataVariationStrategy _metadataVariationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _metadataVariationStrategy = new ProviderMetadataVariationStrategy();
        }

        [TestMethod]
        [DynamicData(nameof(VariationReasonExamples), DynamicDataSourceType.Method)]
        public async Task TracksConfiguredPropertiesForVariationReasons(ProviderVariationContext variationContext,
            VariationReason[] expectedVariationReasons)
        {
            expectedVariationReasons ??= new VariationReason[0];
            
            await _metadataVariationStrategy.DetermineVariations(variationContext, null);

            variationContext
                .VariationReasons
                .OrderBy(_ => _)
                .Should()
                .BeEquivalentTo(expectedVariationReasons.OrderBy(_ => _));

            if (expectedVariationReasons.AnyWithNullCheck())
            {
                variationContext.QueuedChanges
                    .FirstOrDefault()
                    .Should()
                    .BeOfType<MetaDataVariationsChange>();
            }
            else
            {
                variationContext.QueuedChanges
                    .Should()
                    .BeNullOrEmpty();
            }
        }

        private static IEnumerable<object[]> VariationReasonExamples()
        {
            yield return VariationExample(_ => _.Authority = NewRandomString(), VariationReason.AuthorityFieldUpdated);
            yield return VariationExample(_ => _.Name = NewRandomString(), VariationReason.NameFieldUpdated);
            yield return VariationExample(_ => _.URN = NewRandomString(), VariationReason.URNFieldUpdated);
            yield return VariationExample(_ => _.EstablishmentNumber = NewRandomString(), VariationReason.EstablishmentNumberFieldUpdated);
            yield return VariationExample(_ => _.DfeEstablishmentNumber = NewRandomString(), VariationReason.DfeEstablishmentNumberFieldUpdated);
            yield return VariationExample(_ => _.LACode = NewRandomString(), VariationReason.LACodeFieldUpdated);
            yield return VariationExample(_ => _.LegalName = NewRandomString(), VariationReason.LegalNameFieldUpdated);
            yield return VariationExample(_ => _.TrustCode = NewRandomString(), VariationReason.TrustCodeFieldUpdated);
            yield return VariationExample(_ => _.CompaniesHouseNumber = NewRandomString(), VariationReason.CompaniesHouseNumberFieldUpdated);
            yield return VariationExample(_ => _.GroupIdNumber = NewRandomString(), VariationReason.GroupIDFieldUpdated);
            yield return VariationExample(_ => _.RscRegionName = NewRandomString(), VariationReason.RSCRegionNameFieldUpdated);
            yield return VariationExample(_ => _.RscRegionCode = NewRandomString(), VariationReason.RSCRegionCodeFieldUpdated);
            yield return VariationExample(_ => _.GovernmentOfficeRegionName = NewRandomString(), VariationReason.GovernmentOfficeRegionNameFieldUpdated);
            yield return VariationExample(_ => _.GovernmentOfficeRegionCode = NewRandomString(), VariationReason.GovernmentOfficeRegionCodeFieldUpdated);
            yield return VariationExample(_ => _.DistrictName = NewRandomString(), VariationReason.DistrictNameFieldUpdated);
            yield return VariationExample(_ => _.DistrictCode = NewRandomString(), VariationReason.DistrictCodeFieldUpdated);
            yield return VariationExample(_ => _.WardName = NewRandomString(), VariationReason.WardNameFieldUpdated);
            yield return VariationExample(_ => _.WardCode = NewRandomString(), VariationReason.WardCodeFieldUpdated);
            yield return VariationExample(_ => _.CensusWardName = NewRandomString(), VariationReason.CensusWardNameFieldUpdated);
            yield return VariationExample(_ => _.CensusWardCode = NewRandomString(), VariationReason.CensusWardCodeFieldUpdated);
            yield return VariationExample(_ => _.MiddleSuperOutputAreaName = NewRandomString(), VariationReason.MiddleSuperOutputAreaNameFieldUpdated);
            yield return VariationExample(_ => _.MiddleSuperOutputAreaCode = NewRandomString(), VariationReason.MiddleSuperOutputAreaCodeFieldUpdated);
            yield return VariationExample(_ => _.LowerSuperOutputAreaName = NewRandomString(), VariationReason.LowerSuperOutputAreaNameFieldUpdated);
            yield return VariationExample(_ => _.LowerSuperOutputAreaCode = NewRandomString(), VariationReason.LowerSuperOutputAreaCodeFieldUpdated);
            yield return VariationExample(_ => _.ParliamentaryConstituencyName = NewRandomString(), VariationReason.ParliamentaryConstituencyNameFieldUpdated);
            yield return VariationExample(_ => _.ParliamentaryConstituencyCode = NewRandomString(), VariationReason.ParliamentaryConstituencyCodeFieldUpdated);
            yield return VariationExample(_ => _.CountryCode = NewRandomString(), VariationReason.CountryCodeFieldUpdated);
            yield return VariationExample(_ => _.CountryName = NewRandomString(), VariationReason.CountryNameFieldUpdated);
            yield return VariationExample(_ => _.PaymentOrganisationIdentifier = NewRandomString(), VariationReason.PaymentOrganisationIdentifierFieldUpdated);
            yield return VariationExample(_ => _.PaymentOrganisationName = NewRandomString(), VariationReason.PaymentOrganisationNameFieldUpdated);
            yield return VariationExample(_ => _.LocalGovernmentGroupTypeCode = NewRandomString());
            yield return VariationExample(_ => _.LocalGovernmentGroupTypeName = NewRandomString());
            yield return VariationExample(_ => _.ProviderVersionId = NewRandomString());
            yield return VariationExample(_ => _.ProviderId = NewRandomString());
            yield return VariationExample(_ => _.TrustStatus = new RandomEnum<ProviderTrustStatus>());
            yield return VariationExample(_ => _.UKPRN = NewRandomString());
            yield return VariationExample(_ => _.UPIN = NewRandomString());
            yield return VariationExample(_ => _.ProviderType = NewRandomString());
            yield return VariationExample(_ => _.ProviderSubType = NewRandomString());
            yield return VariationExample(_ => _.ProviderProfileIdType = NewRandomString());
            yield return VariationExample(_ => _.NavVendorNo = NewRandomString());
            yield return VariationExample(_ => _.CrmAccountId = NewRandomString());
            yield return VariationExample(_ => _.Status = NewRandomString());
            yield return VariationExample(_ => _.PhaseOfEducation = NewRandomString());
            yield return VariationExample(_ => _.ReasonEstablishmentOpened = NewRandomString());
            yield return VariationExample(_ => _.ReasonEstablishmentClosed = NewRandomString());
            yield return VariationExample(_ => _.Successor = NewRandomString());
            yield return VariationExample(_ => _.Town = NewRandomString());
            yield return VariationExample(_ => _.Postcode = NewRandomString());
            yield return VariationExample(_ =>
            {
                _.Authority = NewRandomString();
                _.CountryName = NewRandomString();
            }, VariationReason.CountryNameFieldUpdated, VariationReason.AuthorityFieldUpdated);
            yield return VariationExample(_ =>
            {
                _.DistrictName = NewRandomString();
                _.Name = NewRandomString();
            }, VariationReason.DistrictNameFieldUpdated, VariationReason.NameFieldUpdated);
        }

        private static object[] VariationExample(Action<Provider> differences, 
            params VariationReason[] variationReasons)
        {
            Provider priorState = NewProvider();
            
            return new object[] { NewProviderVariationContext(_ => _.WithPublishedProvider(NewPublishedProvider(pp => 
                    pp.WithReleased(NewPublishedProviderVersion(ppv => ppv.WithProvider(priorState)))))
                    .WithCurrentState(ProviderCopy(priorState, differences))),
                variationReasons};    
        }
        
        private static string NewRandomString() => new RandomString();

        private static Provider NewProvider()
        {
            return new ProviderBuilder()
                .Build();
        }
        
        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);
            
            return publishedProviderBuilder.Build();
        }

        private static Provider ProviderCopy(Provider provider, Action<Provider> differences = null)
        {
            Provider copy = new ProviderBuilder()
                .WithPropertiesFrom(provider)
                .Build();
            
            differences?.Invoke(copy);
            
            return copy;
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();
            
            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        private static ProviderVariationContext NewProviderVariationContext(Action<ProviderVariationContextBuilder> setUp = null)
        {   
            ProviderVariationContextBuilder variationContextBuilder = new ProviderVariationContextBuilder();
            
            setUp?.Invoke(variationContextBuilder);

            ProviderVariationContext providerVariationContext = variationContextBuilder.Build();

            providerVariationContext.AllPublishedProviderSnapShots = AsDictionary(new PublishedProviderSnapShots(providerVariationContext.PublishedProvider));
            providerVariationContext.AllPublishedProvidersRefreshStates = AsDictionary(providerVariationContext.PublishedProvider);

            return providerVariationContext;
        }

        private static IDictionary<string, PublishedProviderSnapShots> AsDictionary(params PublishedProviderSnapShots[] publishedProviders)
        {
            return publishedProviders.ToDictionary(_ => _.LatestSnapshot.Current.ProviderId);
        }
        private static IDictionary<string, PublishedProvider> AsDictionary(params PublishedProvider[] publishedProviders)
        {
            return publishedProviders.ToDictionary(_ => _.Current.ProviderId);
        }
    }
}