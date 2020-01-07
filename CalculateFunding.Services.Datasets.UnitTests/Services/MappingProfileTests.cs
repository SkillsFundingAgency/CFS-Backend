using System;
using AutoMapper;
using CalculateFunding.Services.Datasets.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Services
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

    }
}
