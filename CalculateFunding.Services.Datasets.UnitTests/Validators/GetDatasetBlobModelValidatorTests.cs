using CalculateFunding.Models.Datasets;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class GetDatasetBlobModelValidatorTests
    {
        [TestMethod]
        public void Validate_GivenMissingDatasetId_ReturnsFalse()
        {
            //Arrange
            GetDatasetBlobModel model = CreateModel();
            model.DatasetId = string.Empty;

            GetDatasetBlobModelValidator validator = new GetDatasetBlobModelValidator();

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
        public void Validate_GivenMissingFilename_ReturnsFalse()
        {
            //Arrange
            GetDatasetBlobModel model = CreateModel();
            model.Filename = string.Empty;

            GetDatasetBlobModelValidator validator = new GetDatasetBlobModelValidator();

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
        public void Validate_GivenVersionIsZero_ReturnsFalse()
        {
            //Arrange
            GetDatasetBlobModel model = CreateModel();
            model.Version = 0;

            GetDatasetBlobModelValidator validator = new GetDatasetBlobModelValidator();

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
            GetDatasetBlobModel model = CreateModel();

            GetDatasetBlobModelValidator validator = new GetDatasetBlobModelValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static GetDatasetBlobModel CreateModel()
        {
            return new GetDatasetBlobModel
            {
                DatasetId = "data-set-id",
                Filename = "any-file.csv",
                Version = 1
            };
        }
    }
}
