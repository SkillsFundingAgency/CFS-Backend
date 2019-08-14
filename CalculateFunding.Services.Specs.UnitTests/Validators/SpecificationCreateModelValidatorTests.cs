using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using CalculateFunding.Common.ApiClient.Policies;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using CalculateFunding.Common.ApiClient.Models;

namespace CalculateFunding.Services.Specs.UnitTests.Validators
{
    [TestClass]
    public class SpecificationCreateModelValidatorTests
    {
        private static string providerVersionId = Guid.NewGuid().ToString();
        private static string fundingPeriodId = Guid.NewGuid().ToString();
        private static string fundingStreamId = Guid.NewGuid().ToString();
        private static string description = "A test description";
        private static string name = "A test name";

        [TestMethod]
        public void Validate_GivenEmptyProviderVersionId_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();
            model.ProviderVersionId = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();

            IProvidersApiClient providersApiClient = CreateProviderApiClient(HttpStatusCode.NotFound);

            SpecificationCreateModelValidator validator = CreateValidator(providersApiClient: providersApiClient);

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
            SpecificationCreateModel model = CreateModel();
            model.FundingPeriodId = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
        public void Validate_GivenFundingPeriodId_FundingPeriodIsNotEmpty_ValidIsTrue()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();
            //model.FundingPeriodId = string.Empty;
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            SpecificationCreateModelValidator validator = CreateValidator(policiesApiClient: policiesApiClient);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void Validate_GivenFundingPeriodId_FundingPeriodDoesntExist_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();

            IPoliciesApiClient policiesApiClient = Substitute.For<IPoliciesApiClient>();

            policiesApiClient
            .GetFundingPeriodById(Arg.Is("1819"))
            .Returns(new ApiResponse<PolicyModels.Period>(HttpStatusCode.OK, new PolicyModels.Period()));


            SpecificationCreateModelValidator validator = CreateValidator(policiesApiClient: policiesApiClient);

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Select(x => x.PropertyName == "FundingPeriodId" && x.ErrorMessage == "Funding period not found")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void Validate_GivenEmptyFundingStreamId_ValidIsFalse()
        {
            //Arrange
            SpecificationCreateModel model = CreateModel();
            model.FundingStreamIds = Enumerable.Empty<string>();

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();
            model.Description = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();
            model.Name = string.Empty;

            SpecificationCreateModelValidator validator = CreateValidator();

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
            SpecificationCreateModel model = CreateModel();

            ISpecificationsRepository repository = CreateSpecificationsRepository(true);

            SpecificationCreateModelValidator validator = CreateValidator(repository);

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
            SpecificationCreateModel model = CreateModel();

            SpecificationCreateModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = validator.Validate(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }


        private static SpecificationCreateModel CreateModel()
        {
            return new SpecificationCreateModel
            {
                ProviderVersionId = providerVersionId,
                FundingPeriodId = fundingPeriodId,
                FundingStreamIds = new List<string>() { fundingStreamId },
                Description = description,
                Name = name
            };
        }

        private static ISpecificationsRepository CreateSpecificationsRepository(bool hasSpecification = false)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
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

        private static SpecificationCreateModelValidator CreateValidator(ISpecificationsRepository repository = null, 
            IProvidersApiClient providersApiClient = null,
            IPoliciesApiClient policiesApiClient = null
            )
        {
            return new SpecificationCreateModelValidator(repository ?? CreateSpecificationsRepository(), 
                providersApiClient ?? CreateProviderApiClient(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                SpecificationsResilienceTestHelper.GenerateTestPolicies());
        }

        private static IPoliciesApiClient CreatePoliciesApiClient()
        {
            IPoliciesApiClient policiesApiClient = Substitute.For<IPoliciesApiClient>(); 

            policiesApiClient
            .GetFundingPeriodById(Arg.Any<string>())
            .Returns(new ApiResponse<PolicyModels.Period>(HttpStatusCode.OK, new PolicyModels.Period { EndDate = DateTimeOffset.Parse("2019-08-31T23:59:59"), Id = "1819", Name = "AY1819", StartDate = DateTimeOffset.Parse("2018-09-01T00:00:00") }));


            return policiesApiClient;
        }
      
    }
}
