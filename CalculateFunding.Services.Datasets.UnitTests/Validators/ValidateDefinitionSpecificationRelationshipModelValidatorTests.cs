using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Services;
using CalculateFunding.Services.Datasets.Services.UnitTests;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class ValidateDefinitionSpecificationRelationshipModelValidatorTests
    {
        private Mock<IDatasetRepository> _datasetRepository;
        private IValidator<ValidateDefinitionSpecificationRelationshipModel> _validator;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;

        public ValidateDefinitionSpecificationRelationshipModelValidatorTests()
        {
            _datasetRepository = new Mock<IDatasetRepository>();
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _validator = new ValidateDefinitionSpecificationRelationshipModelValidator(_datasetRepository.Object, new VisualBasicTypeIdentifierGenerator(), _specificationsApiClient.Object);
        }

        [TestMethod]
        public async Task Validate_GivenMissingSpecificationId_ReturnsFalse()
        {
            //Arrange
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.SpecificationId = string.Empty;

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Missing specification id.");
        }

        [TestMethod]
        public async Task Validate_GivenMissingName_ReturnsFalse()
        {
            //Arrange
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.Name = string.Empty;

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Missing relationship name.");
        }

        [TestMethod]
        public async Task Validate_WhenNoTargetSpecification_ReturnsFalse()
        {
            //Arrange
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel();
            GivenSpecificationNotFound();
            GivenDefinitionsSpecificationRelationships(model.SpecificationId,
                NewDefinitionSpecificationRelationship(r =>
                    r.WithName(NewRandomString())
                    .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithName(NewRandomString())))));

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be($"Target specification - {model.TargetSpecificationId} not found.");
        }

        [TestMethod]
        public async Task Validate_GivenExistingName_ReturnsFalse()
        {
            //Arrange
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel();
            GivenSpecification(model.TargetSpecificationId);
            GivenDefinitionsSpecificationRelationships(model.SpecificationId,
                NewDefinitionSpecificationRelationship(r => 
                    r.WithName(model.Name)
                    .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithName(model.Name)))));

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("You must give a unique relationship name.");
        }

        [TestMethod]
        public async Task Validate_GivenNameVBIndentifierMatchesWithExistingNameVBIndentifier_ReturnsFalse()
        {
            //Arrange
            string relationshipName = NewRandomString();
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel(relationshipName);
            string existingName = relationshipName.Replace("-", string.Empty);
            GivenSpecification(model.TargetSpecificationId);
            GivenDefinitionsSpecificationRelationships(model.SpecificationId,
                NewDefinitionSpecificationRelationship(r =>
                    r.WithName(existingName)
                    .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithName(existingName)))));

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("You must give a unique relationship name.");
        }

        [TestMethod]
        public async Task Validate_GivenSpecificationIsAlreadyReferenedInExistingRelationshipAsTargetSpecification_ReturnsFalse()
        {
            //Arrange
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel();
            GivenSpecification(model.TargetSpecificationId);
            GivenDefinitionsSpecificationRelationships(model.SpecificationId,
                NewDefinitionSpecificationRelationship(r =>
                    r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithName(NewRandomString())
                                                             .WithRelationshipType(DatasetRelationshipType.ReleasedData)
                                                             .WithPublishedSpecificationConfiguration(
                                                                    NewPublishedSpecificationConfiguration(c => c.WithSpecificationId(model.TargetSpecificationId)))))));

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be($"Target specification - {model.TargetSpecificationId} already references in an existing relationship.");
        }

        [TestMethod]
        public async Task Validate_GivenValidData_ReturnsTrue()
        {
            //Arrange
            ValidateDefinitionSpecificationRelationshipModel model = CreateModel();
            GivenSpecification(model.TargetSpecificationId);
            GivenDefinitionsSpecificationRelationships(model.SpecificationId,
                NewDefinitionSpecificationRelationship(r =>
                    r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithName(NewRandomString())))));

            //Act
            ValidationResult result = await _validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();

            result
                .Errors
                .Count
                .Should()
                .Be(0);
        }

        private void GivenDefinitionsSpecificationRelationships(string specificationId, params DefinitionSpecificationRelationship[] definitionSpecificationRelationships)
        {
            _datasetRepository.Setup(x => x.GetDefinitionSpecificationRelationshipsBySpecificationId(specificationId))
                            .ReturnsAsync(definitionSpecificationRelationships);
        }

        private void GivenSpecification(string specificationId)
        {
            _specificationsApiClient.Setup(x => x.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, new SpecificationSummary() { Id = specificationId}, null));
        }

        private void GivenSpecificationNotFound()
        {
            _specificationsApiClient.Setup(x => x.GetSpecificationSummaryById(It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.NotFound, null, null));
        }

        static ValidateDefinitionSpecificationRelationshipModel CreateModel(string name = null)
        {
            return new ValidateDefinitionSpecificationRelationshipModel
            {
                SpecificationId = NewRandomString(),
                Name = name ?? NewRandomString(),
                TargetSpecificationId = NewRandomString()
            };
        }

        static string NewRandomString() => new RandomString();

        static DefinitionSpecificationRelationshipVersion NewDefinitionSpecificationRelationshipVersion(Action<DefinitionSpecificationRelationshipVersionBuilder> setup = null)
        {
            DefinitionSpecificationRelationshipVersionBuilder builder = new DefinitionSpecificationRelationshipVersionBuilder();
            setup?.Invoke(builder);
            return builder.Build();
        }

        static DefinitionSpecificationRelationship NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipBuilder> setup = null)
        {
            DefinitionSpecificationRelationshipBuilder builder = new DefinitionSpecificationRelationshipBuilder();
            setup?.Invoke(builder);
            return builder.Build();
        }

        static PublishedSpecificationConfiguration NewPublishedSpecificationConfiguration(Action<PublishedSpecificationConfigurationBuilder> setup = null)
        {
            PublishedSpecificationConfigurationBuilder builder = new PublishedSpecificationConfigurationBuilder();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
