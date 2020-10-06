using System;
using AutoMapper;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FundingDataZoneProvider = CalculateFunding.Common.ApiClient.FundingDataZone.Models.Provider;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderMappingProfileTests
    {
        [TestMethod]
        public void ResultServiceMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<ProviderVersionsMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .Should()
                .NotThrow("Mapping configuration should be valid for ProviderMappingProfile");
        }

        [TestMethod]
        public void ShouldMapFundingDataZoneProviderPaymentOrganisationUkprnToProviderPaymentOrganisationIdentifier()
        {
            // Arrange
            FundingDataZoneProvider fundingDataZoneProvider = new FundingDataZoneProvider()
            {
                PaymentOrganisationUkprn = new RandomString()
            };

            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<ProviderVersionsMappingProfile>());
            IMapper mapper = config.CreateMapper();

            // Act
            Models.Providers.Provider provider = mapper.Map<Models.Providers.Provider>(fundingDataZoneProvider);

            // Assert
            provider.PaymentOrganisationIdentifier.Should()
                .Be(fundingDataZoneProvider.PaymentOrganisationUkprn);
        }
    }
}
