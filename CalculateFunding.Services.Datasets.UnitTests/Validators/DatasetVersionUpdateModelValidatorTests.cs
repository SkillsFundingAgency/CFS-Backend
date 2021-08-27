using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Services;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class DatasetVersionUpdateModelValidatorTests
    {
        private const string FundingStreamId = "funding-stream-id";
        private const string FundingStreamName = "funding-stream-name";

        [TestMethod]
        public async Task Validate_GivenEmptyDatasetId_ValidIsFalse()
        {
            //Arrange
            DatasetVersionUpdateModel model = CreateModel();
            model.DatasetId = string.Empty;

            DatasetVersionUpdateModelValidator validator = CreateValidator();

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
        public async Task Validate_GivenConverterJobsRunning_ValidateFalse()
        {
            //Arrange
            string definitionSpecificationRelationshipId = new RandomString();
            string jobId = new RandomString();

            DatasetVersionUpdateModel model = CreateModel();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            IJobManagement jobManagement = CreateJobManagement();

            datasetRepository
                .GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(new[] {
                    NewDefinitionSpecificationRelationship(_ => _.WithId(definitionSpecificationRelationshipId))
                });

            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(new[] { 
                    new JobSummary {
                        JobId = jobId,
                        JobType = JobConstants.DefinitionNames.RunConverterDatasetMergeJob,
                        Properties = new Dictionary<string, string> { { "dataset-relationship-id", definitionSpecificationRelationshipId } }
                    },
                    new JobSummary()
                });

            DatasetVersionUpdateModelValidator validator = CreateValidator(datasetRepository: datasetRepository,
                jobManagement: jobManagement);

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

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be($"Unable to upload a new dataset as there is a converter job running id:{jobId}.");
        }

        [TestMethod]
        public void Validate_GivenEmptyFundingStreamId_ValidIsFalse()
        {
            //Arrange
            DatasetVersionUpdateModel model = CreateModel();
            model.FundingStreamId = string.Empty;

            DatasetVersionUpdateModelValidator validator = CreateValidator();

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
            DatasetVersionUpdateModel model = CreateModel();
            model.FundingStreamId = "test-invalid-funding-stream-id";

            DatasetVersionUpdateModelValidator validator = CreateValidator();

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
            DatasetVersionUpdateModel model = CreateModel();

            DatasetVersionUpdateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        static DatasetVersionUpdateModel CreateModel()
        {
            return new DatasetVersionUpdateModel
            {
                Filename = "test-name.xls",
                DatasetId = "test-id",
                FundingStreamId = FundingStreamId,
            };
        }

        static DatasetVersionUpdateModelValidator CreateValidator(
            IPolicyRepository policyRepository = null,
            IJobManagement jobManagement = null,
            IDatasetRepository datasetRepository = null)
        {
            return new DatasetVersionUpdateModelValidator(policyRepository ?? CreatePolicyRepository(),
                jobManagement ?? CreateJobManagement(),
                new DatasetsResiliencePolicies
                {
                    DatasetRepository = Policy.NoOpAsync()
                },
                datasetRepository ?? CreateDatasetRepository()); ;
        }

        static IPolicyRepository CreatePolicyRepository()
        {
            IPolicyRepository repository = Substitute.For<IPolicyRepository>();

            repository
                .GetFundingStreams()
                .Returns(NewFundingStreams());

            return repository;
        }

        static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        static IDatasetRepository CreateDatasetRepository()
        {
            return Substitute.For<IDatasetRepository>();
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

        protected static DefinitionSpecificationRelationship NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipBuilder definitionSpecificationRelationshipBuilder = new DefinitionSpecificationRelationshipBuilder();

            setUp?.Invoke(definitionSpecificationRelationshipBuilder);

            return definitionSpecificationRelationshipBuilder.Build();
        }
    }
}
