using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CalculateFunding.Models.UnitTests
{
    [TestClass]
    public class MappingProfileTests
    {
        [TestMethod]
        public void DatasetsMappingProfile_ShouldBeValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
            Action action = new Action(() =>
            {
                config.AssertConfigurationIsValid();
            });

            //Act/Assert
            action
                .Should().NotThrow("Mapping configuration should be valid for DatasetsMappingProfile");
        }

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
