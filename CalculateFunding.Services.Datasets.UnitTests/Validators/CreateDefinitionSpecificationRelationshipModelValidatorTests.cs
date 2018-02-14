using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class CreateDefinitionSpecificationRelationshipModelValidatorTests
    {
        [TestMethod]
        public void Validate_GivenMissingDatasetDefinitionId_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.DatasetDefinitionId = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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
        public void Validate_GivenMissingSpecificationId_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.SpecificationId = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.Name = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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
        public void Validate_GivenMissingDescription_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.Description = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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
        public void Validate_GivenNameAlreadyExistsn_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();

            IDatasetRepository repository = CreateDatasetRepository(false);

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(repository);

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
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static CreateDefinitionSpecificationRelationshipModelValidator CreateValidator(IDatasetRepository repository = null)
        {
            return new CreateDefinitionSpecificationRelationshipModelValidator(repository ?? CreateDatasetRepository());
        }

        static IDatasetRepository CreateDatasetRepository(bool isValid = true)
        {
            IDatasetRepository repository = Substitute.For<IDatasetRepository>();
            repository
                .GetRelationshipBySpecificationIdAndName(Arg.Is("spec-id"), Arg.Is("test name"))
                .Returns(isValid ? (DefinitionSpecificationRelationship)null: new DefinitionSpecificationRelationship());

            return repository;
        }

        static CreateDefinitionSpecificationRelationshipModel CreateModel()
        {
            return new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = "data-def-id",
                SpecificationId = "spec-id",
                Name = "test name",
                Description = "test description"
            };
        }
    }
}
