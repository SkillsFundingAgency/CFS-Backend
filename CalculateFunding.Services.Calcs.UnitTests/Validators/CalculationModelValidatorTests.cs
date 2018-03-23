using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
            calculation.Name = string.Empty;

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
        async public Task ValidateAsync_WhenCalculationSpecificationIsNull_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.CalculationSpecification = null;

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
        async public Task ValidateAsync_WhenCalculationSpecificationIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.CalculationSpecification.Id = string.Empty;

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
        async public Task ValidateAsync_WhenCalculationSpecificationNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.CalculationSpecification.Name = string.Empty;

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
            calculation.Specification = null;

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
            calculation.Specification.Id = string.Empty;

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
        async public Task ValidateAsync_WhenSpecificationNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Specification.Name = string.Empty;

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
        async public Task ValidateAsync_WhenPeriodIsNull_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Period = null;

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
        async public Task ValidateAsync_WhenPeriodIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Period.Id = string.Empty;

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
        async public Task ValidateAsync_WhenPeriodNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Period.Name = string.Empty;

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
        async public Task ValidateAsync_WhenFuncfingStreamIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingStream.Name = string.Empty;

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
        async public Task ValidateAsync_WhenFundingStreamIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingStream.Id = string.Empty;

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
        async public Task ValidateAsync_WhenFundingStreamNameIsEmpty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingStream.Name = string.Empty;

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
        async public Task ValidateAsync_WhenPoliciesIsNull_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Policies = null;

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
        async public Task ValidateAsync_WhenPoliciesIsempty_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.Policies = new List<Reference>();

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
                Name = "test name",
                CalculationSpecification = new Reference
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "test name"
                },
                Specification = new Models.Results.SpecificationSummary
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "test spec name"
                },
                Period = new Reference
                {
                    Id = "18/19",
                    Name = "2018/2019"
                },
                AllocationLine = new Reference
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "test alloc name"
                },
                Policies = new List<Reference>
                {
                    new Reference()
                },
                FundingStream = new Reference
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "funding stream name"
                }
            };
        }
    }
}
