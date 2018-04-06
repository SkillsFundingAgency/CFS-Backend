using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;

namespace CalculateFunding.Services.Specs.Validators
{
    [TestClass]
    public class CalculationCreateModelValidatorTests
    {
        static string specificationId = Guid.NewGuid().ToString();
        static string allocationLineid = Guid.NewGuid().ToString();
        static string policyId = Guid.NewGuid().ToString();
        static string description = "A test description";
        static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptySpecificationId_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.SpecificationId = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyAllocationLineId_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.AllocationLineId = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();           
        }

        [TestMethod]
        public void Validate_GivenEmptyPolicyId_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.PolicyId = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyDescription_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.Description = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyName_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();
            model.Name = string.Empty;

            CalculationCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenNameAlreadyExists_ValidIsFalse()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            CalculationCreateModelValidator validator = CreateValidator(repository);

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
        public void Validate_GivenValidModel_ValidIsTrue()
        {
            //Arrange
            CalculationCreateModel model = CreateModel();

            CalculationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }


        static CalculationCreateModel CreateModel()
        {
            return new CalculationCreateModel
            {
                SpecificationId = specificationId,
                AllocationLineId = allocationLineid,
                PolicyId = policyId,
                Description = description,
                Name = name
            };
        }

        static ISpecificationsRepository CreateSpecificationsRepository(bool hasCalculation = false)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(specificationId), Arg.Is(name))
                .Returns(hasCalculation ? new Calculation() : null);

            return repository;
        }

        static CalculationCreateModelValidator CreateValidator(ISpecificationsRepository repository = null)
        {
            return new CalculationCreateModelValidator(repository ?? CreateSpecificationsRepository());
        }
    }
}
