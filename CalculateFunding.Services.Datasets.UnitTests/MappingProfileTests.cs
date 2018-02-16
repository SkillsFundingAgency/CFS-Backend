using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Datasets
{
    [TestClass]
    public class MappingProfileTests
    {
        [TestMethod]
        public void FrontendMappingConfigurationIsValid()
        {
            // Arrange
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<DatasetsMappingProfile>());
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
