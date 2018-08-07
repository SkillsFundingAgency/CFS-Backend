using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class CreateNewDatasetModelValidatorTests
    {
        const string DefinitionId = "definition-id";
        const string Filename = "filename.xlsx";
        const string Name = "test-name";
        const string Description = "test description";

        [TestMethod]
        public void Validate_GivenEmptyDefinitionId_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.DefinitionId = string.Empty;

            CreateNewDatasetModelValidator validator = CreateValidator();

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
            CreateNewDatasetModel model = CreateModel();
            model.Description = string.Empty;

            CreateNewDatasetModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyFilename_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.Filename = string.Empty;

            CreateNewDatasetModelValidator validator = CreateValidator();

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
        public void Validate_GivenFilenameWithIncorrectExtension_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.Filename = "Filename.docx";

            CreateNewDatasetModelValidator validator = CreateValidator();

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
            CreateNewDatasetModel model = CreateModel();
            model.Name = string.Empty;

            CreateNewDatasetModelValidator validator = CreateValidator();

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
            CreateNewDatasetModel model = CreateModel();

            IEnumerable<Dataset> datasets = new[]
            {
                new Dataset()
            };

            IDatasetRepository repository = CreateDatasetsRepository(true);
            repository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                .Returns(datasets);

            CreateNewDatasetModelValidator validator = CreateValidator(repository);

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
        public void Validate_GivenInvalidModelWithCsvFile_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
	        model.Filename = "filename.csv";

            CreateNewDatasetModelValidator validator = CreateValidator();

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
                .Should().Be(1);

            result
                .Errors[0]
                .ErrorMessage
                .Should().Contain("Check you have the right file format");
        }

        [TestMethod]
        public void Validate_GivenvalidModelWithXlsFile_ValidIsTrue()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.Filename = "filename.XLS";

            CreateNewDatasetModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenvalidModelWithXlsxFile_ValidIsTrue()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.Filename = "filename.XLSX";

            CreateNewDatasetModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static CreateNewDatasetModelValidator CreateValidator(IDatasetRepository datasetsRepository = null)
        {
            return new CreateNewDatasetModelValidator(datasetsRepository ?? CreateDatasetsRepository());
        }

        static IDatasetRepository CreateDatasetsRepository(bool hasDataset = false)
        {
            IDatasetRepository repository = Substitute.For<IDatasetRepository>();

            repository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                .Returns(hasDataset ? new[] { new Dataset() } : new Dataset[0]);

            return repository;
        }

        static CreateNewDatasetModel CreateModel()
        {
            return new CreateNewDatasetModel
            {
                DefinitionId = DefinitionId,
                Filename = Filename,
                Name = Name,
                Description = Description
            };
        }
    }
}
