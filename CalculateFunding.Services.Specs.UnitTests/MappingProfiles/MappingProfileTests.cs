using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CalculateFunding.Services.Specs.MappingProfiles
{
    [TestClass]
    public class MappingProfileTests
    {
        [TestMethod]
        public void FrontendMappingConfigurationIsValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .ShouldNotThrow("Mapping configuration should be valid");
        }
    }
}
