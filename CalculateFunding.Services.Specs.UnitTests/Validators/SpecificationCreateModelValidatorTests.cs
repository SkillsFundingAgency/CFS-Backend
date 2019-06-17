﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Validators
{
    [TestClass]
    public class SpecificationCreateModelValidatorTests
    {
        private static string providerVersionId = Guid.NewGuid().ToString();
        private static string fundingPeriodId = Guid.NewGuid().ToString();
        private static string fundingStreamId = Guid.NewGuid().ToString();
        private static string description = "A test description";
        private static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptyProviderVersionId_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();
            model.ProviderVersionId = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "ProviderVersionId" && x.ErrorCode == "NotEmptyValidator")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenProviderVersionIdDoesntExist_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();

            IProviderVersionService providerVersionService = CreateProviderVersionService(false);

            SpecificationCreateModelValidator validator = CreateValidator(providerVersionService: providerVersionService);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "ProviderVersionId" && x.ErrorMessage == "Provider version id selected does not exist")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenEmptyFundingPeriodId_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();
            model.FundingPeriodId = string.Empty;

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


        private static SpecificationCreateModel CreateModel()
        {
            return new SpecificationCreateModel
            {
                ProviderVersionId = providerVersionId,
                FundingPeriodId = fundingPeriodId,
                FundingStreamIds = new List<string>() { fundingStreamId },
                Description = description,
                Name = name
            };
        }

        private static ISpecificationsRepository CreateSpecificationsRepository(bool hasSpecification = false)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns(hasSpecification ? new Specification() : null);

            return repository;
        }

        private static IProviderVersionService CreateProviderVersionService(bool providerVersionShouldExist = true)
        {
            IProviderVersionService providerVersionService = Substitute.For<IProviderVersionService>();

            providerVersionService
                .Exists(Arg.Any<string>())
                .Returns(providerVersionShouldExist);

            return providerVersionService;
        }

        private static SpecificationCreateModelValidator CreateValidator(ISpecificationsRepository repository = null, IProviderVersionService providerVersionService = null)
        {
            return new SpecificationCreateModelValidator(repository ?? CreateSpecificationsRepository(), providerVersionService ?? CreateProviderVersionService());
        }
    }
}
