using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Services;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class CreateNewDatasetModelValidatorTests
    {
        const string DefinitionId = "definition-id";
        const string Filename = "filename.xlsx";
        const string Name = "test-name";
        const string Description = "test description";
        const string FundingStreamId = "funding-stream-id";
        const string FundingStreamName = "funding-stream-name";

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
        public void Validate_GivenEmptyFundingStreamId_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.FundingStreamId = string.Empty;

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
        public void Validate_GivenInvalidFundingStreamId_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();
            model.FundingStreamId = "test-invalid-funding-stream-id";

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
                .Be(2);
        }

        [TestMethod]
        public void Validate_GivenDefinitionNotFoundForFundingStreamId_ValidIsFalse()
        {
            //Arrange
            CreateNewDatasetModel model = CreateModel();

            IDatasetRepository datasetRepository = CreateDatasetsRepository(false, false);
            CreateNewDatasetModelValidator validator = CreateValidator(datasetRepository);

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

            IDatasetRepository repository = CreateDatasetsRepository(true, true);
            repository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
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

        static CreateNewDatasetModelValidator CreateValidator(
            IDatasetRepository datasetsRepository = null,
            IPolicyRepository policyRepository = null)
        {
            return new CreateNewDatasetModelValidator(
                datasetsRepository ?? CreateDatasetsRepository(false, true),
                policyRepository ?? CreatePolicyRepository());
        }

        static IDatasetRepository CreateDatasetsRepository(bool hasDataset = false, bool hasDatasetDefinitionForFundingStream = false)
        {
            IDatasetRepository repository = Substitute.For<IDatasetRepository>();

            repository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(hasDataset ? new[] { new Dataset() } : new Dataset[0]);

            repository
                .GetDatasetDefinitionsByFundingStreamId(Arg.Is(FundingStreamId))
                .Returns(hasDatasetDefinitionForFundingStream ? 
                new[] { new Models.Datasets.Schema.DatasetDefinationByFundingStream() { Id = DefinitionId } } 
                : Enumerable.Empty<Models.Datasets.Schema.DatasetDefinationByFundingStream>());

            return repository;
        }

        static IPolicyRepository CreatePolicyRepository()
        {
            IPolicyRepository repository = Substitute.For<IPolicyRepository>();

            repository
                .GetFundingStreams()
                .Returns(NewFundingStreams());

            return repository;
        }

        protected static IEnumerable<PoliciesApiModels.FundingStream> NewFundingStreams() =>
            new List<PoliciesApiModels.FundingStream>
            {
                NewApiFundingStream(_ => _.WithId(FundingStreamId).WithName(FundingStreamName))
            };

        protected static PoliciesApiModels.FundingStream NewApiFundingStream(
            Action<PolicyFundingStreamBuilder> setUp = null)
        {
            PolicyFundingStreamBuilder fundingStreamBuilder = new PolicyFundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        static CreateNewDatasetModel CreateModel()
        {
            return new CreateNewDatasetModel
            {
                DefinitionId = DefinitionId,
                Filename = Filename,
                Name = Name,
                Description = Description,
                FundingStreamId = FundingStreamId
            };
        }
    }
}
