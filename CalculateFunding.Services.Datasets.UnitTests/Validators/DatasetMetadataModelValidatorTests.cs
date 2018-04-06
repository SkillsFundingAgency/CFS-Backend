using CalculateFunding.Models.Datasets;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class DatasetMetadataModelValidatorTests
    {
        [TestMethod]
        public void Validate_GivenEmptyDataDefinitionId_ValidIsFalse()
        {
            //Arrange
            DatasetMetadataModel model = CreateModel();
            model.DataDefinitionId = string.Empty;

            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

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
        public void Validate_GivenEmptyAuthorName_ValidIsFalse()
        {
            //Arrange
            DatasetMetadataModel model = CreateModel();
            model.AuthorName = string.Empty;

            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

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
        public void Validate_GivenEmptyAuthorId_ValidIsFalse()
        {
            //Arrange
            DatasetMetadataModel model = CreateModel();
            model.AuthorId = string.Empty;

            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

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
        public void Validate_GivenEmptyDatasetId_ValidIsFalse()
        {
            //Arrange
            DatasetMetadataModel model = CreateModel();
            model.DatasetId = string.Empty;

            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

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
            DatasetMetadataModel model = CreateModel();
            model.Name = string.Empty;

            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

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
            DatasetMetadataModel model = CreateModel();
            model.Description = string.Empty;

            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

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
            DatasetMetadataModel model = CreateModel();
           
            DatasetMetadataModelValidator validator = new DatasetMetadataModelValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static DatasetMetadataModel CreateModel()
        {
            return new DatasetMetadataModel
            {
                Name = "test-name",
                DatasetId = "test-id",
                AuthorId = "test-author-id",
                AuthorName = "test-author-name",
                DataDefinitionId = "test-definition-id",
                Description = "test description",
            };
        }
    }
}
