using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Linq.Expressions;

namespace CalculateFunding.Services.Specs.Validators
{
    [TestClass]
    public class PolicyCreateModelValidatorTests
    {
        static string specificationId = Guid.NewGuid().ToString();
        static string allocationLineid = Guid.NewGuid().ToString();
        static string parentPolicyId = Guid.NewGuid().ToString();
        static string description = "A test description";
        static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptySpecificationId_ValidIsFalse()
        {
            //Arrange
            PolicyCreateModel model = CreateModel();
            model.SpecificationId = string.Empty;

            PolicyCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyParentPolicyId_ValidIsTrue()
        {
            //Arrange
            PolicyCreateModel model = CreateModel();
            model.ParentPolicyId = string.Empty;

            PolicyCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenAParentPolicyIdAndPolicyExists_ValidIsTrue()
        {
            //Arrange
            PolicyCreateModel model = CreateModel();
            model.ParentPolicyId = parentPolicyId;

            ISpecificationsRepository repository = CreateSpecificationsRepository(false, true);

            PolicyCreateModelValidator validator = CreateValidator(repository);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenAParentPolicyIdAndPolicyDoesNotExist_ValidIsFalse()
        {
            //Arrange
            PolicyCreateModel model = CreateModel();
            model.ParentPolicyId = parentPolicyId;

            ISpecificationsRepository repository = CreateSpecificationsRepository(false, false);

            PolicyCreateModelValidator validator = CreateValidator(repository);

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
            PolicyCreateModel model = CreateModel();
            model.Description = string.Empty;

            PolicyCreateModelValidator validator = CreateValidator();

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
            PolicyCreateModel model = CreateModel();
            model.Name = string.Empty;

            PolicyCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenParentPolicyIdParentDoesNotExistExists_ValidIsFalse()
        {
            //Arrange
            PolicyCreateModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            PolicyCreateModelValidator validator = CreateValidator(repository);

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
            PolicyCreateModel model = CreateModel();

            PolicyCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static PolicyCreateModel CreateModel(string policyId = null)
        {
            return new PolicyCreateModel
            {
                SpecificationId = specificationId,
                ParentPolicyId = policyId,
                Description = description,
                Name = name
            };
        }

        static ISpecificationsRepository CreateSpecificationsRepository(bool hasPolicy = false, bool parentPolicyExists = true)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetPolicyBySpecificationIdAndPolicyName(Arg.Is(specificationId), Arg.Is(name))
                .Returns(hasPolicy ? new Policy() : null);

            repository
                .GetPolicyBySpecificationIdAndPolicyId(Arg.Is(specificationId), Arg.Is(parentPolicyId))
                .Returns(parentPolicyExists ? new Policy() : null);

            return repository;
        }

        static PolicyCreateModelValidator CreateValidator(ISpecificationsRepository repository = null)
        {
            return new PolicyCreateModelValidator(repository ?? CreateSpecificationsRepository());
        }
    }
}
