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
        async public Task ValidateAsync_WhenPeriodIsNull_ValidIsFalse()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingPeriod = null;

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
            calculation.FundingPeriod.Id = string.Empty;

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
            calculation.FundingPeriod.Name = string.Empty;

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
        async public Task ValidateAsync_WhenFundingStreamIsNull_ValidIsTrue()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingStream = null;

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        async public Task ValidateAsync_WhenFundingStreamIsHasValue_ValidIsTrue()
        {
            //Arrange
            Calculation calculation = CreateCalculation();
            calculation.FundingStream = new Reference("fsid", "Funding Stream Name");

            CalculationModelValidator validator = new CalculationModelValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(calculation);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        async public Task ValidateAsync_WhenFundingStreamIsEmpty_ValidIsFalse()
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
                SpecificationId = "test spec name",
                FundingPeriod = new Reference
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
