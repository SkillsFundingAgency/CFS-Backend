using System;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Validators
{
    [TestClass]
    public class PolicyEditModelValidatorTests
    {
        static string specificationId = Guid.NewGuid().ToString();
        static string PolicyId = Guid.NewGuid().ToString();
        static string parentPolicyId = Guid.NewGuid().ToString();
        static string description = "A test description";
        static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptySpecificationId_ValidIsFalse()
        {
            //Arrange
            PolicyEditModel model = CreateModel();
            model.SpecificationId = string.Empty;

            PolicyEditModelValidator validator = CreateValidator();

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
            PolicyEditModel model = CreateModel();
            model.ParentPolicyId = string.Empty;

            PolicyEditModelValidator validator = CreateValidator();

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
            PolicyEditModel model = CreateModel();
            model.ParentPolicyId = parentPolicyId;

            ISpecificationsRepository repository = CreateSpecificationsRepository(false, true);

            PolicyEditModelValidator validator = CreateValidator(repository);

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
            PolicyEditModel model = CreateModel();
            model.ParentPolicyId = parentPolicyId;

            ISpecificationsRepository repository = CreateSpecificationsRepository(false, false);

            PolicyEditModelValidator validator = CreateValidator(repository);

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
            PolicyEditModel model = CreateModel();
            model.Description = string.Empty;

            PolicyEditModelValidator validator = CreateValidator();

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
            PolicyEditModel model = CreateModel();
            model.Name = string.Empty;

            PolicyEditModelValidator validator = CreateValidator();

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
            PolicyEditModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            PolicyEditModelValidator validator = CreateValidator(repository);

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
        public void Validate_GivenPolicyIdIsNullOrEmpty_ValidIsFalse()
        {
            //Arrange
            PolicyEditModel model = CreateModel();
            model.PolicyId = "";

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            PolicyEditModelValidator validator = CreateValidator(repository);

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
            PolicyEditModel model = CreateModel();

            PolicyEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static PolicyEditModel CreateModel(string policyId = null)
        {
            return new PolicyEditModel
            {
                PolicyId = PolicyId,
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

        static PolicyEditModelValidator CreateValidator(ISpecificationsRepository repository = null)
        {
            return new PolicyEditModelValidator(repository ?? CreateSpecificationsRepository());
        }
    }
}
