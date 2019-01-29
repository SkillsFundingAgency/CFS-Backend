using System;
using CalculateFunding.Models.Results;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.Validators
{
	[TestClass]
	public class MasterProviderModelValidatorTests
	{
		[TestMethod]
		public void Validate_GivenEstablishmentNameIsEmpty_ShouldReturnValidationError()
		{
			// Arrange
			MasterProviderModelValidator validatorUnderTest = new MasterProviderModelValidator();
			MasterProviderModel providerModelToValidate = GetMasterProviderModel(m => m.MasterProviderName = string.Empty);

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(providerModelToValidate);

			// Assert
			validationResult
				.Errors
				.Should().NotBeNullOrEmpty();

			validationResult
				.Errors
				.Count.Should().Be(1);

			validationResult
				.Errors[0]
				.ErrorMessage
				.Should().Be("Urn: 123432 - Establisment name column was empty");

			validationResult
				.IsValid
				.Should().BeFalse();
		}

		[TestMethod]
		public void Validate_GivenLocalAuthorityNameIsEmpty_ShouldReturnValidationError()
		{
			// Arrange
			MasterProviderModelValidator validatorUnderTest = new MasterProviderModelValidator();
			MasterProviderModel providerModelToValidate = GetMasterProviderModel(m => m.MasterLocalAuthorityName = string.Empty);

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(providerModelToValidate);

			// Assert
			validationResult
				.Errors
				.Should().NotBeNullOrEmpty();

			validationResult
				.Errors
				.Count.Should().Be(1);

			validationResult.Errors[0].ErrorMessage
				.Should().Be("Urn: 123432 - Local Authority name column was empty");

			validationResult
				.IsValid
				.Should().BeFalse();
		}

		[TestMethod]
		public void Validate_GivenModelIsValid_ShouldReturnEmptyValidationResult()
		{
			// Arrange
			MasterProviderModelValidator validatorUnderTest = new MasterProviderModelValidator();
			MasterProviderModel providerModelToValidate = GetMasterProviderModel();

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(providerModelToValidate);

			// Assert
			validationResult
				.Errors
				.Should().BeEmpty();

			validationResult
				.IsValid
				.Should().BeTrue();
		}

		[TestMethod]
		public void Validate_GivenProviderTypeIsEmpty_ShouldReturnValidationError()
		{
			// Arrange
			MasterProviderModelValidator validatorUnderTest = new MasterProviderModelValidator();
			MasterProviderModel providerModelToValidate = GetMasterProviderModel(m => m.MasterProviderTypeGroupName = string.Empty);

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(providerModelToValidate);

			// Assert
			validationResult
				.Errors
				.Should().NotBeNullOrEmpty();

			validationResult
				.Errors
				.Count.Should().Be(1);

			validationResult
				.Errors[0]
				.ErrorMessage
				.Should().Be("Urn: 123432 - Provider Type Group Name column was empty");

			validationResult
				.IsValid
				.Should().BeFalse();
		}

		[TestMethod]
		public void Validate_GivenProviderSubtypeIsEmpty_ShouldReturnValidationError()
		{
			// Arrange
			MasterProviderModelValidator validatorUnderTest = new MasterProviderModelValidator();
			MasterProviderModel providerModelToValidate = GetMasterProviderModel(m => m.MasterProviderTypeName = string.Empty);

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(providerModelToValidate);

			// Assert
			validationResult
				.Errors
				.Should().NotBeNullOrEmpty();

			validationResult
				.Errors
				.Count.Should().Be(1);

			validationResult
				.Errors[0]
				.ErrorMessage
				.Should().Be("Urn: 123432 - Provider Type Name column was empty");

			validationResult
				.IsValid
				.Should().BeFalse();
		}

		private MasterProviderModel GetMasterProviderModel(Action<MasterProviderModel> changeMasterProvider = null)
		{
			MasterProviderModel masterProviderModel = new MasterProviderModel()
			{
				MasterReasonEstablishmentClosed = EstablishmentClosedReason.AcademyConverter,
				MasterProviderName = "Provider Name",
				MasterCRMAccountId = "Crm account id",
				MasterDateClosed = new DateTime(2019, 1, 1),
				MasterDateOpened = new DateTime(2018, 1, 1),
				MasterDfEEstabNo = "Establish No",
				MasterDfELAEstabNo = "LaEstablish No",
				MasterLocalAuthorityCode = "La Code",
				MasterLocalAuthorityName = "La Name",
				MasterNavendorNo = "Nav vendor no",
				MasterPhaseOfEducation = "Phase of education",
				MasterProviderLegalName = "Legal Name",
				MasterProviderStatusName = "Status Name",
				MasterProviderTypeGroupName = "Type group name",
				MasterProviderTypeName = "Provider type name",
				MasterReasonEstablishmentOpened = EstablishmentOpenedReason.AcademyConverter,
				MasterSuccessor = "12345678",
				MasterUKPRN = "123321",
				MasterUPIN = "123312",
				MasterURN = "123432"
			};

			changeMasterProvider?.Invoke(masterProviderModel);

			return masterProviderModel;
		}
	}
}
