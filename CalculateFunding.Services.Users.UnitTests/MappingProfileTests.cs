using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public class MappingProfileTests
    {
        [TestMethod]
        public void UsersMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<UsersMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for UsersMappingProfile");
        }
    }
}
