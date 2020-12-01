using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Validators
{
    [TestClass]
    public class SpecificationEditModelValidatorTests
    {
        private static string fundingPeriodId = Guid.NewGuid().ToString();
        private static string providerVersionId = Guid.NewGuid().ToString();
        private static string fundingStreamId = Guid.NewGuid().ToString();
        private static string description = "A test description";
        private static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptyProviderVersionId_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.ProviderVersionId = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "ProviderVersionId" && x.ErrorCode == "NotEmptyValidator")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenProviderVersionIdDoesntExist_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.SpecificationId = "specId";

            IProvidersApiClient providersApiClient = CreateProviderApiClient(HttpStatusCode.NotFound);

            SpecificationEditModelValidator validator = CreateValidator(providersApiClient: providersApiClient);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "ProviderVersionId" && x.ErrorMessage == "Provider version id selected does not exist")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenEmptyFundingPeriodId_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.FundingPeriodId = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

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
        public void Validate_GivenEmptyProviderSnapshotId_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.SpecificationId = "specId";

            IPoliciesApiClient policiesApiClient = Substitute.For<IPoliciesApiClient>();

            policiesApiClient
            .GetFundingPeriodById(Arg.Any<string>())
            .Returns(new ApiResponse<PolicyModels.FundingPeriod>(HttpStatusCode.OK, new PolicyModels.FundingPeriod()));

            policiesApiClient
            .GetFundingConfiguration(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ApiResponse<PolicyModels.FundingConfig.FundingConfiguration>(HttpStatusCode.OK, new PolicyModels.FundingConfig.FundingConfiguration { ProviderSource = ProviderSource.FDZ }));

            SpecificationEditModelValidator validator = CreateValidator(policiesApiClient: policiesApiClient);

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
        public void Validate_GivenNonExistingFundingPeriodId_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.FundingPeriodId = "A Non Existing ID";
            model.SpecificationId = "specId";
            SpecificationEditModelValidator validator = CreateValidator(policiesApiClient: CreatePoliciesApiClient(HttpStatusCode.NotFound));

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
        public void Validate_GivenEmptyDescription_ValidIsTrue()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.Description = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

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

        [TestMethod]
        public void Validate_GivenEmptyName_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.Name = string.Empty;
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

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
            SpecificationEditModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            SpecificationEditModelValidator validator = CreateValidator(repository);

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
            SpecificationEditModel model = CreateModel();
            model.SpecificationId = "specId";

            SpecificationEditModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenCoreProviderVersionUpdatesUseLatestForNonFDZProviderSource_ValidIsFalse()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.SpecificationId = "specId";
            model.CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest;

            ISpecificationsRepository repository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient(providerSource: ProviderSource.CFS);

            SpecificationEditModelValidator validator = CreateValidator(repository, policiesApiClient: policiesApiClient);

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

            result
                .Errors.Select(x => x.ErrorMessage)
                .Should()
                .Contain("CoreProviderVersionUpdates - UseLatest is not valid for provider source - CFS");
        }

        [TestMethod]
        public void Validate_GivenCoreProviderVersionUpdatesUseLatestForFDZProviderSource_ValidIsTrue()
        {
            //Arrange
            SpecificationEditModel model = CreateModel();
            model.SpecificationId = "specId";
            model.CoreProviderVersionUpdates = CoreProviderVersionUpdates.UseLatest;
            model.ProviderSnapshotId = 1;

            ISpecificationsRepository repository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient(providerSource:ProviderSource.FDZ);

            SpecificationEditModelValidator validator = CreateValidator(repository, policiesApiClient: policiesApiClient);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        private static SpecificationEditModel CreateModel()
        {
            return new SpecificationEditModel
            {
                ProviderVersionId = providerVersionId,
                FundingPeriodId = fundingPeriodId,
                Description = description,
                Name = name
            };
        }

        private static ISpecificationsRepository CreateSpecificationsRepository(bool hasSpecification = false)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<DocumentEntity<Specification>, bool>>>())
                .Returns(hasSpecification ? new Specification() : null);

            return repository;
        }

        private static IProvidersApiClient CreateProviderApiClient(HttpStatusCode statusCode = HttpStatusCode.NoContent)
        {
            IProvidersApiClient providerApiClient = Substitute.For<IProvidersApiClient>();

            providerApiClient
                .DoesProviderVersionExist(Arg.Any<string>())
                .Returns(statusCode);

            return providerApiClient;
        }

        private static IPoliciesApiClient CreatePoliciesApiClient(HttpStatusCode statusCode = HttpStatusCode.NoContent, ProviderSource providerSource = ProviderSource.CFS)
        {
            IPoliciesApiClient policiesApiClient = Substitute.For<IPoliciesApiClient>();

            policiesApiClient
                .GetFundingPeriodById(Arg.Any<string>())
                .Returns(new ApiResponse<PolicyModels.FundingPeriod>(statusCode));

            policiesApiClient
            .GetFundingConfiguration(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ApiResponse<PolicyModels.FundingConfig.FundingConfiguration>(HttpStatusCode.OK, new PolicyModels.FundingConfig.FundingConfiguration() {ProviderSource = providerSource }));

            return policiesApiClient;
        }

        private static SpecificationEditModelValidator CreateValidator(
            ISpecificationsRepository repository = null,
            IProvidersApiClient providersApiClient = null,
            IPoliciesApiClient policiesApiClient = null)
        {
            return new SpecificationEditModelValidator(
                repository ?? CreateSpecificationsRepository(),
                providersApiClient ?? CreateProviderApiClient(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                SpecificationsResilienceTestHelper.GenerateTestPolicies());
        }
    }
}
