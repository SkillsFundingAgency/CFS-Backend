using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
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
    public class UpdateDefinitionSpecificationRelationshipModelValidatorTests
    {
        private readonly string _specificationId;
        private readonly string _targetSpecificationId;
        private readonly IEnumerable<uint> _calculationIds;
        private readonly IEnumerable<uint> _fundingLineIds;
        private readonly string _fundingStreamId;
        private readonly string _fundingPeriodId;
        private readonly string _relationshipId;
        private readonly string _relationshipName;
        private const string TemplateVersion = "1.0";
        private readonly VisualBasicTypeIdentifierGenerator _typeIdentifierGenerator;

        public UpdateDefinitionSpecificationRelationshipModelValidatorTests()
        {
            _specificationId = NewRandomString();
            _targetSpecificationId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _calculationIds = new[] { NewRandomUint() };
            _fundingLineIds = new[] { NewRandomUint() };
            _relationshipId = NewRandomString();
            _relationshipName = NewRandomString();
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        [TestMethod]
        public async Task Validate_GivenMissingDescription_ReturnsFalse()
        {
            //Arrange
            UpdateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.Description = string.Empty;

            UpdateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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
        public async Task Validate_GivenRemovalOfReferencedFundingLines_ReturnsFalse()
        {
            //Arrange
            UpdateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.CalculationIds = new uint[0];

            Mock<ICalcsRepository> calcsRepository = CreateCalcsRepository(true);

            UpdateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(calcsRepository: calcsRepository.Object);

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

            ErrorMessageShouldContain(result,
                $"Unable to remove {_calculationIds.First()} as it is in use.");
        }

        [TestMethod]
        public async Task Validate_GivenFundingLinesAndCalculationsMissingFromTemplate_ReturnsFalse()
        {
            //Arrange
            UpdateDefinitionSpecificationRelationshipModel model = CreateModel();

            Mock<IPoliciesApiClient> policiesApiClient = CreatePoliciesApiClient(false);

            UpdateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator(policiesApiClient: policiesApiClient.Object);

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

            ErrorMessageShouldContain(result, 
                $"The following funding lines not found in the metadata for fundingStream - {_fundingStreamId}, fundingperiod - {_fundingPeriodId} and template id - {TemplateVersion}: {string.Join(",", _fundingLineIds)}");
            
            ErrorMessageShouldContain(result,
                $"The following calculations not found in the metadata for fundingStream - {_fundingStreamId}, fundingperiod - {_fundingPeriodId} and template id - {TemplateVersion}: {string.Join(",", _calculationIds)}");
        }


        [TestMethod]
        public async Task Validate_GivenNoFundingLinesAndCalculations_ReturnsFalse()
        {
            //Arrange
            UpdateDefinitionSpecificationRelationshipModel model = CreateModel();
            model.CalculationIds = null;
            model.FundingLineIds = null;

            UpdateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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

            ErrorMessageShouldContain(result,
                "At least one funding line or calculation must be provided for the ReleasedData relationship type");
        }

        [TestMethod]
        public async Task Validate_GivenValidModel_ReturnsTrue()
        {
            //Arrange
            UpdateDefinitionSpecificationRelationshipModel model = CreateModel();

            UpdateDefinitionSpecificationRelationshipModelValidator validator = CreateValidator();

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


        private UpdateDefinitionSpecificationRelationshipModelValidator CreateValidator(
            IDatasetRepository repository = null,
            IPoliciesApiClient policiesApiClient = null,
            ISpecificationsApiClient specificationsApiClient = null,
            ICalcsRepository calcsRepository = null)
        {
            return new UpdateDefinitionSpecificationRelationshipModelValidator(
                policiesApiClient ?? CreatePoliciesApiClient().Object,
                specificationsApiClient ?? CreateSpecificationsApiClient().Object,
                calcsRepository ?? CreateCalcsRepository().Object,
                repository ?? CreateDatasetRepository().Object,
                DatasetsResilienceTestHelper.GenerateTestPolicies());
        }

        private Mock<ICalcsRepository> CreateCalcsRepository(bool referenced = false)
        {
            Mock<ICalcsRepository> repository = new Mock<ICalcsRepository>();

            repository.Setup(x => x.GetCurrentCalculationsBySpecificationId(_specificationId))
                .ReturnsAsync(!referenced ? null : new[] { new CalculationResponseModel { SourceCode = $"return Datasets.{_typeIdentifierGenerator.GenerateIdentifier(_relationshipName)}.{CodeGenerationDatasetTypeConstants.CalculationPrefix}_{_calculationIds.First()}_{_calculationIds.First()}()" } });

            return repository;
        }

        private Mock<IDatasetRepository> CreateDatasetRepository(bool isValid = true)
        {
            Mock<IDatasetRepository> repository = new Mock<IDatasetRepository>();
            
            repository.Setup(x => x.GetDefinitionSpecificationRelationshipById(_relationshipId))
                .ReturnsAsync(!isValid ? null : new DefinitionSpecificationRelationship { 
                    Id = _relationshipId,
                    Name = _relationshipName,
                    Current = new DefinitionSpecificationRelationshipVersion {
                        RelationshipType = DatasetRelationshipType.ReleasedData,
                        PublishedSpecificationConfiguration = new PublishedSpecificationConfiguration
                        {
                            Calculations = _calculationIds.Select(_ => new PublishedSpecificationItem { 
                                TemplateId = _, 
                                SourceCodeName = _.ToString(), 
                                Name = _.ToString() }),
                            FundingLines = _fundingLineIds.Select(_ => new PublishedSpecificationItem { 
                                TemplateId = _, 
                                SourceCodeName = _.ToString(), 
                                Name = _.ToString() }),
                        }
                    } 
                });

            return repository;
        }

        private Mock<IPoliciesApiClient> CreatePoliciesApiClient(bool isValid = true)
        {
            Mock<IPoliciesApiClient> client = new Mock<IPoliciesApiClient>();

            client.Setup(x => x.GetDistinctTemplateMetadataContents(_fundingStreamId, _fundingPeriodId, TemplateVersion))
                .ReturnsAsync(!isValid ? new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, 
                new TemplateMetadataDistinctContents()) : 
                new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, new TemplateMetadataDistinctContents
                {
                    Calculations = _calculationIds.Select(_ => new TemplateMetadataCalculation { TemplateCalculationId = _ }),
                    FundingLines = _fundingLineIds.Select(_ => new TemplateMetadataFundingLine { TemplateLineId = _ })
                }));

            return client;
        }

        private Mock<ISpecificationsApiClient> CreateSpecificationsApiClient(bool isValid = true)
        {
            Mock<ISpecificationsApiClient> client = new Mock<ISpecificationsApiClient>();

            string fundingStream = NewRandomString();

            client.Setup(x => x.GetSpecificationSummaryById(_specificationId))
                .ReturnsAsync(!isValid ? null : new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, NewSpecificationSummary(_ => 
                _.WithId(_specificationId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithFundingStreamIds(new[] { _fundingStreamId })
                .WithTemplateVersions((_fundingStreamId, TemplateVersion)))));

            client.Setup(x => x.GetSpecificationSummaryById(_targetSpecificationId))
                .ReturnsAsync(!isValid ? null : new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, NewSpecificationSummary(_ =>
                _.WithId(_targetSpecificationId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithFundingStreamIds(new[] { _fundingStreamId })
                .WithTemplateVersions((_fundingStreamId, TemplateVersion)))));

            return client;
        }

        private UpdateDefinitionSpecificationRelationshipModel CreateModel()
        {
            return new UpdateDefinitionSpecificationRelationshipModel
            {
                CalculationIds = _calculationIds,
                FundingLineIds = _fundingLineIds,
                SpecificationId = _specificationId,
                TargetSpecificationId = _targetSpecificationId,
                RelationshipId = _relationshipId,
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
