using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Services;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    [TestClass]
    public class DatasetVersionUpdateModelValidatorTests
    {
        const string FundingStreamId = "funding-stream-id";
        const string FundingStreamName = "funding-stream-name";

        [TestMethod]
        public void Validate_GivenEmptyDatasetId_ValidIsFalse()
        {
            //Arrange
            DatasetVersionUpdateModel model = CreateModel();
            model.DatasetId = string.Empty;

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
    IPolicyRepository policyRepository = null)
        {
            return new DatasetVersionUpdateModelValidator(
                policyRepository ?? CreatePolicyRepository());
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
    }
}
