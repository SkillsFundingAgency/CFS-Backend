using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class MappingProfileTests
    {

        [TestMethod]
        public void ResultsMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<ResultsMappingProfile>());
            Action action = () =>
            {
                config.AssertConfigurationIsValid();
            };

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for ResultsMappingProfile");
        }

      
    }
}
