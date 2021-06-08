using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class CreateDefinitionSpecificationRelationshipModelValidatorTests
    {
        [TestMethod]
        public async Task Validate_GivenMissingDatasetDefinitionId_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.DatasetDefinitionId = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

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
        public async Task Validate_GivenDatasetDefinitionIsNotCoverterEnabled_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.ConverterEnabled = true;

            IDatasetRepository repository = CreateDatasetRepository(true);

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(repository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should()
                .Be(2);
        }

        [TestMethod]
        public async Task Validate_GivenMissingSpecificationId_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.SpecificationId = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

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
        public async Task Validate_GivenMissingName_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.Name = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

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
        public async Task Validate_GivenMissingDescription_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.Description = string.Empty;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

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
        public async Task Validate_GivenNameAlreadyExistsn_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();

            IDatasetRepository repository = CreateDatasetRepository(false);

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(repository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

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
        public async Task Validate_GivenValidModel_ReturnsTrue()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

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
            repository
                .GetDatasetDefinition(Arg.Is("data-def-id"))
                .Returns(!isValid ? (DatasetDefinition)null : new DatasetDefinition());

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
