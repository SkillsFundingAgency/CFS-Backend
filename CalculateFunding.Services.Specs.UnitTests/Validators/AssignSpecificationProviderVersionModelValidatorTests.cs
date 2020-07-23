using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.UnitTests.Validators
{
    [TestClass]
    public class AssignSpecificationProviderVersionModelValidatorTests
    {
        private static readonly string ProviderVersionId = new RandomString();
        private static readonly string SpecificationId = new RandomString();

        [TestMethod]
        public async Task Validate_GivenEmptyProviderVersionId_ValidIsFalse()
        {
            //Arrange
            AssignSpecificationProviderVersionModel model = CreateModel(SpecificationId);
            AssignSpecificationProviderVersionModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Where(x => x.PropertyName == "ProviderVersionId" && x.ErrorCode == "NotEmptyValidator" && x.ErrorMessage == "Null or Empty ProviderVersionId provided")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task Validate_GivenProviderVersionNotFoundForProviderVersionId_ValidIsFalse()
        {
            //Arrange
            AssignSpecificationProviderVersionModel model = CreateModel(SpecificationId, ProviderVersionId);
            IProvidersApiClient providersApiClient = CreateProviderApiClient();
            AssignSpecificationProviderVersionModelValidator validator = CreateValidator(providersApiClient: providersApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Where(x => x.PropertyName == "ProviderVersionId" && x.ErrorMessage == "Provider version id specified does not exist")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task Validate_GivenEmptySpecificationId_ValidIsFalse()
        {
            //Arrange
            AssignSpecificationProviderVersionModel model = CreateModel(null, ProviderVersionId);
            AssignSpecificationProviderVersionModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Where(x => x.PropertyName == "SpecificationId" && x.ErrorCode == "NotEmptyValidator" && x.ErrorMessage == "Null or Empty SpecificationId provided")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task Validate_GivenSpecificationNotFoundForSpecificationId_ValidIsFalse()
        {
            //Arrange
            AssignSpecificationProviderVersionModel model = CreateModel(SpecificationId, ProviderVersionId);
            AssignSpecificationProviderVersionModelValidator validator = CreateValidator();

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Where(x => x.PropertyName == "SpecificationId" && x.ErrorMessage == $"Specification not found for SpecificationId - {SpecificationId}")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task Validate_GivenSpecificationProviderSourceIsNotFDZ_ValidIsFalse()
        {
            //Arrange
            AssignSpecificationProviderVersionModel model = CreateModel(SpecificationId, ProviderVersionId);
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository(true, ProviderSource.CFS);
            AssignSpecificationProviderVersionModelValidator validator = CreateValidator(specificationsRepository);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors.Where(x => x.ErrorMessage == $"Specification ProviderSource is not set to FDZ")
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task Validate_GivenSpecificationWithProviderSourceFDZAndProviderVersionExists_ValidIsTrue()
        {
            //Arrange
            AssignSpecificationProviderVersionModel model = CreateModel(SpecificationId, ProviderVersionId);
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository(true, ProviderSource.FDZ);
            IProvidersApiClient providersApiClient = CreateProviderApiClient(HttpStatusCode.OK);
            AssignSpecificationProviderVersionModelValidator validator = CreateValidator(specificationsRepository, providersApiClient);

            //Act
            ValidationResult result = await validator.ValidateAsync(model);

            //Assert
            result
                .IsValid
                .Should()
                .BeTrue();

            result
                .Errors
                .Count()
                .Should()
                .Be(0);
        }

        private AssignSpecificationProviderVersionModel CreateModel(string specificationId = null, string providerVersionId = null)
        {
            return new AssignSpecificationProviderVersionModel(specificationId, providerVersionId);
        }

        private static ISpecificationsRepository CreateSpecificationsRepository(bool hasSpecification = false, ProviderSource providerSource = ProviderSource.FDZ)
        {
            ISpecificationsRepository repository = Substitute.For<ISpecificationsRepository>();

            repository
                .GetSpecificationById(SpecificationId)
                .Returns(hasSpecification ? new Specification() { Current = new SpecificationVersion() { ProviderSource = providerSource } } : null);

            return repository;
        }

        private static IProvidersApiClient CreateProviderApiClient(HttpStatusCode statusCode = HttpStatusCode.NotFound)
        {
            IProvidersApiClient providerApiClient = Substitute.For<IProvidersApiClient>();

            providerApiClient
                .DoesProviderVersionExist(ProviderVersionId)
                .Returns(statusCode);

            return providerApiClient;
        }

        private static AssignSpecificationProviderVersionModelValidator CreateValidator(ISpecificationsRepository repository = null, IProvidersApiClient providersApiClient = null
            )
        {
            return new AssignSpecificationProviderVersionModelValidator(repository ?? CreateSpecificationsRepository(),
                providersApiClient ?? CreateProviderApiClient(),
                SpecificationsResilienceTestHelper.GenerateTestPolicies());
        }
    }
}
