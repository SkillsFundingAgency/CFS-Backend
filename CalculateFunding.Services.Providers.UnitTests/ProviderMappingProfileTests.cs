﻿using System;
using AutoMapper;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Providers.MappingProfiles;
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

        [TestMethod]
        public void SholdMapCurrentProviderVersionToCurrentProviderVersionMetadata()
        {
            // Arrange
            string fundingStreamId = NewRandomString();
            CurrentProviderVersion currentProviderVersion = NewCurrentProviderVersion(_ => _.ForFundingStreamId(fundingStreamId));

            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<ProviderVersionsMappingProfile>());
            IMapper mapper = config.CreateMapper();

            // Act
            CurrentProviderVersionMetadata metadata = mapper.Map<CurrentProviderVersionMetadata>(currentProviderVersion);

            // Assert
            metadata
                .FundingStreamId
                .Should()
                .Be(fundingStreamId);

            metadata
               .ProviderVersionId
               .Should()
               .Be(currentProviderVersion.ProviderVersionId);

            metadata
              .ProviderSnapshotId
              .Should()
              .Be(currentProviderVersion.ProviderSnapshotId);
        }

        private CurrentProviderVersion NewCurrentProviderVersion(Action<CurrentProviderVersionBuilder> setUp = null)
        {
            CurrentProviderVersionBuilder currentProviderVersionBuilder = new CurrentProviderVersionBuilder();

            setUp?.Invoke(currentProviderVersionBuilder);

            return currentProviderVersionBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
