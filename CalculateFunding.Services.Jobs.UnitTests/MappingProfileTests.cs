using System;
using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Jobs
{
    [TestClass]
    public class MappingProfileTests
    {
        [TestMethod]
        public void JobsMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<JobsMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for JobsMappingProfile");
        }
    }
}
