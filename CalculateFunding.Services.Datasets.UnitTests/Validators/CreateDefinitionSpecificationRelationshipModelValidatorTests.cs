using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.UnitTests.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class CreateDefinitionSpecificationRelationshipModelValidatorTests
    {
        private string _specificationId;
        private string _datasetDefinitionId;
        private string _relationshipName;

        public CreateDefinitionSpecificationRelationshipModelValidatorTests()
        {
            _specificationId = NewRandomString();
            _datasetDefinitionId = NewRandomString();
            _relationshipName = NewRandomString();
        }

        [TestMethod]
        public async Task Validate_GivenMissingDatasetDefinitionId_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.Uploaded;
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

            IDatasetRepository repository = CreateDatasetRepository(true).Object;

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
        public async Task Validate_GivenNameAlreadyExists_ReturnsFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();

            IDatasetRepository repository = CreateDatasetRepository(false).Object;

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

        [TestMethod]
        public async Task Validate_GivenRelationshipTypeReleaseDataWithNoTargetSpecificationId_ReturnFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.ReleasedData;
            model.TargetSpecificationId = null;

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            ErrorMessageShouldContain(result, "Target specification must be provided for relationship type - ReleasedData.");
            ErrorMessageShouldContain(result, "At least one fundingline or calculation must be provided for relationship type - ReleasedData.");
        }

        [TestMethod]
        public async Task Validate_GivenRelationshipTypeReleaseDataWhenTargetSpecificationNotFound_ReturnFalse()
        {
            //Arrange
            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.ReleasedData;
            model.TargetSpecificationId = NewRandomString();
            model.FundingLineIds = new[] { NewRandomUint() };

            Mock<ISpecificationsApiClient> specificationApiClient = CreateSpecificationsApiClient();
            specificationApiClient.Setup(x => x.GetSpecificationSummaryById(model.TargetSpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.NotFound));

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(
                specificationsApiClient: specificationApiClient.Object);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            ErrorMessageShouldContain(result, $"Target specification - {model.TargetSpecificationId} not found.");
        }

        [TestMethod]
        public async Task Validate_GivenRelationshipTypeReleaseDataWhenTemplateMetadataNotFound_ReturnFalse()
        {
            //Arrange
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            uint fundingLineTemplateId = NewRandomUint();

            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.ReleasedData;
            model.TargetSpecificationId = NewRandomString();
            model.FundingLineIds = new[] { fundingLineTemplateId };

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTemplateVersions((fundingStreamId, templateVersion)));

            Mock<ISpecificationsApiClient> specificationApiClient = CreateSpecificationsApiClient();
            specificationApiClient.Setup(x => x.GetSpecificationSummaryById(model.TargetSpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            Mock<IPoliciesApiClient> policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.Setup(x => x.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.NotFound));

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(
                specificationsApiClient: specificationApiClient.Object,
                policiesApiClient: policiesApiClient.Object);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            ErrorMessageShouldContain(result, $"Template metadata for fundingstream - {fundingStreamId}, fundingPeriodId - {fundingPeriodId} and templateId - {templateVersion} not found.");
        }

        [TestMethod]
        public async Task Validate_GivenRelationshipTypeReleaseDataWithUnknownFundingLineIds_ReturnFalse()
        {
            //Arrange
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            uint fundingLineTemplateId = NewRandomUint();

            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.ReleasedData;
            model.TargetSpecificationId = NewRandomString();
            model.FundingLineIds = new[] { fundingLineTemplateId };

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTemplateVersions((fundingStreamId, templateVersion)));

            Mock<ISpecificationsApiClient> specificationApiClient = CreateSpecificationsApiClient();
            specificationApiClient.Setup(x => x.GetSpecificationSummaryById(model.TargetSpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            Mock<IPoliciesApiClient> policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.Setup(x => x.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, new TemplateMetadataDistinctContents()));

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(
                specificationsApiClient: specificationApiClient.Object,
                policiesApiClient: policiesApiClient.Object);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            ErrorMessageShouldContain(result, $"The following funding lines not found in the metadata for fundingStream - {fundingStreamId}, fundingperiod - {fundingPeriodId} and template id - {templateVersion}: {fundingLineTemplateId}");
        }

        [TestMethod]
        public async Task Validate_GivenRelationshipTypeReleaseDataWithUnknownCalculationIds_ReturnFalse()
        {
            //Arrange
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            uint calculationTemplateId = NewRandomUint();

            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.ReleasedData;
            model.TargetSpecificationId = NewRandomString();
            model.CalculationIds = new[] { calculationTemplateId };

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTemplateVersions((fundingStreamId, templateVersion)));

            Mock<ISpecificationsApiClient> specificationApiClient = CreateSpecificationsApiClient();
            specificationApiClient.Setup(x => x.GetSpecificationSummaryById(model.TargetSpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            Mock<IPoliciesApiClient> policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.Setup(x => x.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, new TemplateMetadataDistinctContents()));

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(
                specificationsApiClient: specificationApiClient.Object,
                policiesApiClient: policiesApiClient.Object);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            ErrorMessageShouldContain(result, $"The following calculations not found in the metadata for fundingStream - {fundingStreamId}, fundingperiod - {fundingPeriodId} and template id - {templateVersion}: {calculationTemplateId}");
        }

        [TestMethod]
        public async Task Validate_GivenRelationshipTypeReleaseDataWithExistingFundingLineIdsAndCalculationIds_ReturnTrue()
        {
            //Arrange
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            uint calculationTemplateId1 = NewRandomUint();
            uint calculationTemplateId2 = NewRandomUint();
            uint fundingLineTemplateId1 = NewRandomUint();
            uint fundingLineTemplateId2 = NewRandomUint();

            CreateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.RelationshipType = DatasetRelationshipType.ReleasedData;
            model.TargetSpecificationId = NewRandomString();
            model.DatasetDefinitionId = null;
            model.CalculationIds = new[] { calculationTemplateId1, calculationTemplateId2 };
            model.FundingLineIds = new[] { fundingLineTemplateId1, fundingLineTemplateId2 };

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTemplateVersions((fundingStreamId, templateVersion)));

            TemplateMetadataDistinctContents metadataContents = new TemplateMetadataDistinctContents()
            {
                Calculations = new[]
                {
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationTemplateId2},
                    new TemplateMetadataCalculation(){TemplateCalculationId = NewRandomUint()},
                    new TemplateMetadataCalculation(){TemplateCalculationId = calculationTemplateId1},
                },
                FundingLines = new[]
                {
                    new TemplateMetadataFundingLine{ TemplateLineId = fundingLineTemplateId1},
                    new TemplateMetadataFundingLine{TemplateLineId= fundingLineTemplateId2},
                    new TemplateMetadataFundingLine{TemplateLineId = NewRandomUint()}
                }
            };
            Mock<ISpecificationsApiClient> specificationApiClient = CreateSpecificationsApiClient();
            specificationApiClient.Setup(x => x.GetSpecificationSummaryById(model.TargetSpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            Mock<IPoliciesApiClient> policiesApiClient = CreatePoliciesApiClient();
            policiesApiClient.Setup(x => x.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, metadataContents));

            CreateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(
                specificationsApiClient: specificationApiClient.Object,
                policiesApiClient: policiesApiClient.Object);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        private void ErrorMessageShouldContain(ValidationResult result, string errorMessage)
        {
            IEnumerable<string> errorMessages = result.Errors.Select(x => x.ErrorMessage);
            errorMessages
                .Should()
                .Contain(errorMessage);
        }


        private CreateDefinitionSpecificationRelationshipModelValidator CreateValidator(
            IDatasetRepository repository = null,
            IPoliciesApiClient policiesApiClient = null,
            ISpecificationsApiClient specificationsApiClient = null)
        {
            return new CreateDefinitionSpecificationRelationshipModelValidator(
                repository ?? CreateDatasetRepository().Object,
                policiesApiClient ?? CreatePoliciesApiClient().Object,
                specificationsApiClient ?? CreateSpecificationsApiClient().Object,
                DatasetsResilienceTestHelper.GenerateTestPolicies());
        }

        private Mock<IDatasetRepository> CreateDatasetRepository(bool isValid = true)
        {
            Mock<IDatasetRepository> repository = new Mock<IDatasetRepository>();
            repository.Setup(x => x.GetRelationshipBySpecificationIdAndName(_specificationId, _relationshipName))
                .ReturnsAsync(isValid ? (DefinitionSpecificationRelationship)null : new DefinitionSpecificationRelationship());
            repository.Setup(x => x.GetDatasetDefinition(_datasetDefinitionId))
                .ReturnsAsync(!isValid ? (DatasetDefinition)null : new DatasetDefinition());

            return repository;
        }

        private Mock<IPoliciesApiClient> CreatePoliciesApiClient()
        {
            return new Mock<IPoliciesApiClient>();
        }

        private Mock<ISpecificationsApiClient> CreateSpecificationsApiClient()
        {
            return new Mock<ISpecificationsApiClient>();
        }

        private CreateDefinitionSpecificationRelationshipModel CreateModel()
        {
            return new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = _datasetDefinitionId,
                SpecificationId = _specificationId,
                Name = _relationshipName,
                Description = NewRandomString()
            };
        }

        private string NewRandomString() => new RandomString();
        private uint NewRandomUint() => (uint)new RandomNumberBetween(0, int.MaxValue);

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }
    }
}
