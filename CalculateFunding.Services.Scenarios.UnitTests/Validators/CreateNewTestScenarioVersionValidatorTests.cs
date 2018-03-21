using CalculateFunding.Models.Scenarios;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Scenarios.Validators
{
    [TestClass]
    public class CreateNewTestScenarioVersionValidatorTests
    {
        [TestMethod]
        public void Validate_GivenMissingSpecificationId_ReturnsFalse()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.SpecificationId = string.Empty;

            CreateNewTestScenarioVersionValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenMissingName_ReturnsFalse()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Name = string.Empty;

            CreateNewTestScenarioVersionValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenMissingScenario_ReturnsFalse()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Scenario = string.Empty;

            CreateNewTestScenarioVersionValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenValidModel_ReturnsTrue()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            CreateNewTestScenarioVersionValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();

            result
                .Errors
                .Any()
                .Should()
                .BeFalse();
        }

        static CreateNewTestScenarioVersionValidator CreateValidator()
        {
            return new CreateNewTestScenarioVersionValidator();
        }

        static CreateNewTestScenarioVersion CreateModel()
        {
            return new CreateNewTestScenarioVersion
            {
                SpecificationId = "spec-id",
                Name = "the name",
                Scenario = "the scenario"
            };
        }
    }
}
