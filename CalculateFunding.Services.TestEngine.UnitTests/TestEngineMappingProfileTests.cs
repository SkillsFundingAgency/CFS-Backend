using System;
using AutoMapper;
using CalculateFunding.Services.TestEngine.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.TestEngine.UnitTests
{
    [TestClass]
    public class TestEngineMappingProfileTests
    {
        [TestMethod]
        public void TestEngineMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<TestEngineMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for TestEngineMappingProfile");
        }
    }
}
