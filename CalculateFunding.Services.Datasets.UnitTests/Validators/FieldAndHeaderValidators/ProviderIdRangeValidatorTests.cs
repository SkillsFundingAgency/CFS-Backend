using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Models.Datasets.Schema;
using FluentAssertions;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
    [TestClass]
    public class ProviderIdRangeValidatorTests
    {
        private static readonly DatasetUploadCellReference AnyCellReference = new DatasetUploadCellReference(1, 1);

        [TestMethod]
        public void ValidateField_GivenProviderIdentifierIsInRange_ShouldReturnCorrectResult()
        {
			// Arrange
			ProviderIdRangeValidator validatorUnderTest = new ProviderIdRangeValidator();
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(0, 2);

			FieldDefinition definition = new FieldDefinition
			{
				Name = "UKPRN",
				Id = "1100003",
				IdentifierFieldType = IdentifierFieldType.UKPRN,
				Type = FieldType.String
			};

			Field field = new Field(anyCellReference, "1100003", definition);

			// Act
			FieldValidationResult result = validatorUnderTest.ValidateField(field);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(100000001)]
		public void ValidateField_GivenProviderIdentifierIsNotInRange_ShouldReturnFieldError(int fieldValue)
		{
			// Arrange
			ProviderIdRangeValidator validatorUnderTest = new ProviderIdRangeValidator();
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(0, 2);

			FieldDefinition definition = new FieldDefinition
			{
				Name = "UKPRN",
				Id = "1",
				IdentifierFieldType = IdentifierFieldType.UKPRN,
				Type = FieldType.String
			};

			Field field = new Field(anyCellReference, fieldValue.ToString(), definition);

			// Act
			FieldValidationResult result = validatorUnderTest.ValidateField(field);

			// Assert
			result.Should().NotBeNull();
			result.ReasonOfFailure
				.Should().Be(FieldValidationResult.ReasonForFailure.ProviderIdNotInCorrectFormat);
		}

		[TestMethod]
		public void ValidateField_GivenProviderIdentifierHasDataTypeMismatch_ShouldReturnFieldError()
		{
			// Arrange
			ProviderIdRangeValidator validatorUnderTest = new ProviderIdRangeValidator();
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(0, 2);

			FieldDefinition definition = new FieldDefinition
			{
				Name = "UKPRN",
				Id = "ab",
				IdentifierFieldType = IdentifierFieldType.UKPRN,
				Type = FieldType.String
			};

			Field field = new Field(anyCellReference, "ab", definition);

			// Act
			FieldValidationResult result = validatorUnderTest.ValidateField(field);

			// Assert
			result.Should().NotBeNull();
			result.ReasonOfFailure
				.Should().Be(FieldValidationResult.ReasonForFailure.DataTypeMismatch);
		}
	}
}
