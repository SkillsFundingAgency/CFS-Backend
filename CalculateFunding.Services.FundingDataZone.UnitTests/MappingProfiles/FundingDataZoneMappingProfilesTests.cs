using AutoMapper;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.MappingProfiles;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.FundingDataZone.UnitTests.MappingProfiles
{
    [TestClass]
    public class FundingDataZoneMappingProfilesTests
    {
        [TestMethod]
        public void FundingDataZoneMappingProfiles_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<FundingDataZoneMappingProfiles>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            // Act/Assert
            action
                .Should()
                .NotThrow("Mapping configuration should be valid for FundingDataZoneMappingProfiles");
        }

        [TestMethod]
        public void FundingDataZoneMappingProfiles_ShouldMapProviderPredesssors()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<FundingDataZoneMappingProfiles>());
            var mapper = new Mapper(config);

            // Act
            Provider provider = mapper.Map<Provider>(new PublishingAreaProvider() { Predecessors = "p1,p2"});

            //Assert
            provider
                .Predecessors
                .Should()
                .HaveCount(2);
           provider
                .Predecessors
                .Should()
                .BeEquivalentTo(new[] { "p1", "p2"});
        }

        [TestMethod]
        public void FundingDataZoneMappingProfiles_ShouldMapProviderSuccessors()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<FundingDataZoneMappingProfiles>());
            var mapper = new Mapper(config);

            // Act
            Provider provider = mapper.Map<Provider>(new PublishingAreaProvider() { Successors = "p1,p2" });

            //Assert
            provider
                .Successors
                .Should()
                .HaveCount(2);
            provider
                 .Successors
                 .Should()
                 .BeEquivalentTo(new[] { "p1", "p2" });
        }
    }
}
