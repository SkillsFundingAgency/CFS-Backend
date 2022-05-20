using System;
using AutoMapper;
using CalculateFunding.Services.CalcEngine.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calculator
{  

    [TestClass]
    public class CalcEngineMappingProfileTests
    {
        [TestMethod]
        public void CalcEngineMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<CalcEngineMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid(); 
            });

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for CalcEngineMappingProfile");
        }
    }
}
