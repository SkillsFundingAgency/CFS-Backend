using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Datasets.Schema;
using FluentAssertions;
using NSubstitute;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;
using System.Collections.Generic;
using System;
using CalculateFunding.Services.Datasets.Services;
using Polly;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class DatasetDefinitionValidatorTests
    {
        private const string FundingStreamId = "funding-stream-id";
        private const string FundingStreamName = "funding-stream-name";
        private const string DatasetDefinitionName = "dataset-definition-name";
        private const string DatasetDefinitionId = "dataset-definition-id";


        [TestMethod]
        public void Validate_GivenEmptyFundingStreamId_ValidIsFalse()
        {
            //Arrange
            DatasetDefinition model = CreateModel();
            model.FundingStreamId = string.Empty;

            DatasetDefinitionValidator validator = CreateValidator();

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
            DatasetDefinition model = CreateModel();
            model.FundingStreamId = "test-invalid-funding-stream-id";

            DatasetDefinitionValidator validator = CreateValidator();

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
        public void Validate_GivenDuplicateDatasetDefinitionName_ValidIsFalse()
        {
            //Arrange
            DatasetDefinition model = CreateModel();
            model.Name = DatasetDefinitionName;
            model.Id = DatasetDefinitionId;

            DatasetDefinitionValidator validator = CreateValidator();

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
        public void Validate_GivenDuplicateFieldDefinitionName_ValidIsFalse()
        {
            //Arrange
            DatasetDefinition model = CreateModel();
            model.TableDefinitions = new List<TableDefinition> {
                new TableDefinition
                {
                    FieldDefinitions = new List<FieldDefinition>
                    {
                        new FieldDefinition
                        {
                            Name = "my function name"
                        },
                        new FieldDefinition
                        {
                            Name = "My Function Name"
                        }
                    }
                }
            };

            DatasetDefinitionValidator validator = CreateValidator();

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

        static DatasetDefinition CreateModel()
        {
            return new DatasetDefinition
            {
                FundingStreamId = FundingStreamId,
            };
        }

        static DatasetDefinitionValidator CreateValidator(
    IPolicyRepository policyRepository = null,
    IDatasetRepository datasetRepository = null)
        {
            return new DatasetDefinitionValidator(
                policyRepository ?? CreatePolicyRepository(),
                datasetRepository ?? CreateDatasetRepository(),
                new DatasetsResiliencePolicies
                {
                    DatasetRepository = Policy.NoOpAsync()
                });
        }

        static IPolicyRepository CreatePolicyRepository()
        {
            IPolicyRepository repository = Substitute.For<IPolicyRepository>();

            repository
                .GetFundingStreams()
                .Returns(NewFundingStreams());

            return repository;
        }

        static IDatasetRepository CreateDatasetRepository()
        {
            IDatasetRepository repository = Substitute.For<IDatasetRepository>();

            repository
                .DatasetExistsWithGivenName(DatasetDefinitionName, DatasetDefinitionId)
                .Returns(true);

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

    }
}
