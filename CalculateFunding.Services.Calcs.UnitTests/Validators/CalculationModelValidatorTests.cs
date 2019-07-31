using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calcs.Validators
{
    [TestClass]
    public class CalculationModelValidatorTests
    {
        [TestMethod]
        async public Task ValidateAsync_WhenCalcIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Id = string.Empty;

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        async public Task ValidateAsync_WhenCalcNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Current.Name = string.Empty;

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        async public Task ValidateAsync_WhenSpecificationIsNull_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = null;

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }

        [TestMethod]
        async public Task ValidateAsync_WhenSpecificationIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = string.Empty;

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();
        }


        [TestMethod]
        async public Task ValidateAsync_WhenGivenValidModel_ValidIsTrue()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static Calculation CreateCalculation()
        {
            return new Calculation
            {
                Id = Guid.NewGuid().ToString(),
                Current = new CalculationVersion
                {
                    Name = "test name",
                },
                SpecificationId = "test spec name",
            };
        }
    }
}
