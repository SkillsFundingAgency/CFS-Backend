using System;
using AutoMapper;
using CalculateFunding.Services.Specs.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class MappingProfileTests
    {

        [TestMethod]
        public void SpecificationsMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for SpecificationsMappingProfile");
        }


    }
}
