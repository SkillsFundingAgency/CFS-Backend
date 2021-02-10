using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.UnitTests.Validators
{
    [TestClass]
    public class ObsoleteItemValidatorTests
    {
        [TestMethod]
        public async Task ValidateAsync_WhenIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            ObsoleteItem model = CreateModel();
            model.Id = string.Empty;

            ObsoleteItemValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == "Null or empty obsolete item id provided.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationIdIsEmpty_ValidIsFalse()
        {
            //Arrange
            ObsoleteItem model = CreateModel();
            model.SpecificationId = string.Empty;

            ObsoleteItemValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == "Null or empty specification id provided.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationIdsIsEmpty_ValidIsFalse()
        {
            //Arrange
            ObsoleteItem model = CreateModel();
            model.CalculationIds = null;

            ObsoleteItemValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == "Atleast one calculation id must be provided.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenSpecificationNotFound_ValidIsFalse()
        {
            //Arrange
            ObsoleteItem model = CreateModel();
            ObsoleteItemValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == "Failed to find specification for provided specification id.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenCalculationNotFound_ValidIsFalse()
        {
            //Arrange
            string calculationId = NewRandomString();
            ObsoleteItem model = CreateModel(calculationId: calculationId);
            ObsoleteItemValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result.Errors
               .Should()
               .Contain(_ => _.ErrorMessage == $"Failed to find calculation for provided calculation id - { calculationId}.");
        }

        [TestMethod]
        public async Task ValidateAsync_WhenValidModel_ValidIsTrue()
        {
            //Arrange
            string specificationId = NewRandomString();
            string calculationId = NewRandomString();
            ObsoleteItem model = CreateModel(specificationId, calculationId);
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            ICalculationsRepository calculationsRepository = CreateCalculationRepository();

            ObsoleteItemValidator validator = CreateValidator(calculationsRepository, specificationsApiClient);
            specificationsApiClient
               .GetSpecificationSummaryById(specificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, new SpecificationSummary()));
            calculationsRepository
                .GetCalculationById(calculationId)
                .Returns(new Models.Calcs.Calculation());

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        private static ObsoleteItemValidator CreateValidator(
            ICalculationsRepository calculationRepository = null,
            ISpecificationsApiClient specificationsApiClient = null)
        {
            return new ObsoleteItemValidator(
                calculationRepository ?? CreateCalculationRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                CalcsResilienceTestHelper.GenerateTestPolicies());
        }

        private static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private static ICalculationsRepository CreateCalculationRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static ObsoleteItem CreateModel(string specificationId = null, string calculationId = null)
        {
            return new ObsoleteItem
            {
                Id = NewRandomString(),
                SpecificationId = specificationId ?? NewRandomString(),
                CalculationIds = new[] { calculationId ?? NewRandomString() }
            };
        }

        private static string NewRandomString() => new RandomString();
    }
}
