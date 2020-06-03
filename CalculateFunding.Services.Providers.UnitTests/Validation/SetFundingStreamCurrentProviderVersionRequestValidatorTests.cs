using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Providers.UnitTests.Validation
{
    [TestClass]
    public class SetFundingStreamCurrentProviderVersionRequestValidatorTests
    {
        private Mock<IPoliciesApiClient> _policies;
        private Mock<IProviderVersionsMetadataRepository> _providerVersions;
        private string _existingFundingStreamId;
        private string _existingProviderVersionId;

        private SetFundingStreamCurrentProviderVersionRequestValidator _validator;

        [TestInitialize]
        public void SetUp()
        {
            _policies = new Mock<IPoliciesApiClient>();
            _providerVersions = new Mock<IProviderVersionsMetadataRepository>();

            _validator = new SetFundingStreamCurrentProviderVersionRequestValidator(_providerVersions.Object,
                _policies.Object,
                new ProvidersResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    ProviderVersionMetadataRepository = Policy.NoOpAsync()
                });

            _existingFundingStreamId = NewRandomString();
            _existingProviderVersionId = NewRandomString();

            _policies.Setup(_ => _.GetFundingStreamById(_existingFundingStreamId))
                .ReturnsAsync(new ApiResponse<FundingStream>(HttpStatusCode.OK, new FundingStream()));
            _providerVersions.Setup(_ => _.GetProviderVersionMetadata(_existingProviderVersionId))
                .ReturnsAsync(new ProviderVersionMetadata());
        }

        [TestMethod]
        public async Task ValidatesForMissingParameters()
        {
            ValidationResult validationResult = await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ =>
                _.WithFundingStreamId(null)
                    .WithProviderVersionId(null)));

            ThenTheValidationResultsAre(validationResult,
                ("FundingStreamId", "'Funding Stream Id' must not be empty."),
                ("ProviderVersionId", "'Provider Version Id' must not be empty."));
        }

        [TestMethod]
        public async Task ValidatesForMissingEntities()
        {
            string missingProviderVersionId = NewRandomString();
            string missingFundingStreamId = NewRandomString();

            ValidationResult validationResult = await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ =>
                _.WithFundingStreamId(missingFundingStreamId)
                    .WithProviderVersionId(missingProviderVersionId)));

            ThenTheValidationResultsAre(validationResult,
                ("FundingStreamId", $"No funding stream located with Id {missingFundingStreamId}"),
                ("ProviderVersionId", $"No provider version located with Id {missingProviderVersionId}"));
        }

        [TestMethod]
        public async Task IsValidIfParametersSuppliedAndEntitiesExist()
        {
            ValidationResult validationResult = await WhenTheRequestIsValidated(NewOtherwiseValidRequest());

            validationResult
                .IsValid
                .Should()
                .BeTrue();
        }

        private SetFundingStreamCurrentProviderVersionRequest NewOtherwiseValidRequest(
            Action<SetFundingStreamCurrentProviderVersionRequestBuilder> overrides = null)
        {
            SetFundingStreamCurrentProviderVersionRequestBuilder requestBuilder = new SetFundingStreamCurrentProviderVersionRequestBuilder()
                .WithFundingStreamId(_existingFundingStreamId)
                .WithProviderVersionId(_existingProviderVersionId);

            overrides?.Invoke(requestBuilder);

            return requestBuilder.Build();
        }

        private async Task<ValidationResult> WhenTheRequestIsValidated(SetFundingStreamCurrentProviderVersionRequest request)
            => await _validator.ValidateAsync(request);

        private void ThenTheValidationResultsAre(ValidationResult validationResult,
            params (string name, string message)[] expectedResults)
        {
            validationResult.Errors.Count
                .Should()
                .Be(expectedResults?.Length ?? 0);

            foreach ((string name, string message) expectedResult in expectedResults)
            {
                validationResult
                    .Errors
                    .Count(_ => _.PropertyName == expectedResult.name &&
                                _.ErrorMessage == expectedResult.message)
                    .Should()
                    .Be(1);
            }
        }

        private static string NewRandomString() => new RandomString();
    }
}