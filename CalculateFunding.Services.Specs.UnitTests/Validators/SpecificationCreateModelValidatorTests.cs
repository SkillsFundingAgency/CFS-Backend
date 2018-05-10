using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CalculateFunding.Services.Specs.Validators
{
    [TestClass]
    public class SpecificationCreateModelValidatorTests
    {
        static string academicYearId = Guid.NewGuid().ToString();
        static string fundingStreamId = Guid.NewGuid().ToString();
        static string description = "A test description";
        static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptyAcademicYearId_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();
            model.AcademicYearId = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();
            model.FundingStreamIds = Enumerable.Empty<string>();

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();
            model.Description = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();
            model.Name = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            SpecificationCreateModelValidator validator = CreateValidator(repository);

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
            SpecificationCreateModel model = CreateModel();

            SpecificationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }


        static SpecificationCreateModel CreateModel()
        {
            return new SpecificationCreateModel
            {
                AcademicYearId = academicYearId,
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

        static SpecificationCreateModelValidator CreateValidator(ISpecificationsRepository repository = null)
        {
            return new SpecificationCreateModelValidator(repository ?? CreateSpecificationsRepository());
        }
    }
}
