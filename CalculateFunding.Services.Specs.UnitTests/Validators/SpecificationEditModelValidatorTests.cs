using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public class SpecificationEditModelValidatorTests
    {
        static string fundingPeriodId = Guid.NewGuid().ToString();
        static string fundingStreamId = Guid.NewGuid().ToString();
        static string description = "A test description";
        static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptyFundingPeriodId_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.FundingPeriodId = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyFundingStreamId_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.FundingStreamIds = Enumerable.Empty<string>();
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

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
            SpecificationEditModel model = CreateModel();
            model.Description = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

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
            SpecificationEditModel model = CreateModel();
            model.Name = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

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
            SpecificationEditModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            SpecificationEditModelValidator validator = CreateValidator(repository);

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
            SpecificationEditModel model = CreateModel();
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }


        static SpecificationEditModel CreateModel()
        {
            return new SpecificationEditModel
            {
                FundingPeriodId = fundingPeriodId,
                FundingStreamIds = new List<string>() { fundingStreamId },
                Description = description,
                Name = name
            };
        }

        static ISpecificationsRepository CreateSpecificationsRepository(bool hasSpecification = false)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns(hasSpecification ? new Specification() : null);

            return repository;
        }

        static SpecificationEditModelValidator CreateValidator(ISpecificationsRepository repository = null)
        {
            return new SpecificationEditModelValidator(repository ?? CreateSpecificationsRepository());
        }
    }
}
