using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
	[TestClass]
	public class DatatypeMismatchFieldValidatorTests
	{
		[TestMethod]
		public void ValidateField_WhenFieldValueIsNull_ShouldIgnoreAndReturnCorrectResult()
		{
			// Arrange
			DatatypeMismatchFieldValidator validatorUnderTest = new DatatypeMismatchFieldValidator();
			FieldDefinition fieldDefinition = new FieldDefinition()
			{
				Description = "The name of the provider",
				Id = "1100004",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = null,
				Minimum = null,
				Name = "Provider Name",
				Required = false,
				Type = FieldType.Boolean
			};

			const int anyRow = 1;
			const int anyColumn = 1;

			// Act
			FieldValidationResult result =
				validatorUnderTest
					.ValidateField(new Field(new DatasetUploadCellReference(anyRow, anyColumn), null, fieldDefinition));

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void ValidateField_WhenFieldValueDoesNotConformToExpectedType_ShouldReturnAFailureResult()
		{
			// Arrange
			DatatypeMismatchFieldValidator validatorUnderTest = new DatatypeMismatchFieldValidator();
			FieldDefinition fieldDefinition = new FieldDefinition()
			{
				Description = "The name of the provider",
				Id = "1100004",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = null,
				Minimum = null,
				Name = "Provider Name",
				Required = false,
				Type = FieldType.Boolean
			};

			const int anyRow = 1;
			const int anyColumn = 1;

			// Act
			FieldValidationResult result =
				validatorUnderTest
					.ValidateField(new Field(new DatasetUploadCellReference(anyRow, anyColumn), "WrongDatatype", fieldDefinition));


			// Assert
			result
				.Should().NotBeNull();

			result
				.ReasonOfFailure
				.Should().Be(FieldValidationResult.ReasonForFailure.DataTypeMismatch);
		}

		[TestMethod]
		public void ValidateField_WhenFieldValueDoesConformToExpectedType_ShouldReturnNull()
		{
			// Arrange
			DatatypeMismatchFieldValidator validatorUnderTest = new DatatypeMismatchFieldValidator();
			FieldDefinition fieldDefinition = new FieldDefinition()
			{
				Description = "The name of the provider",
				Id = "1100004",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = null,
				Minimum = null,
				Name = "Provider Name",
				Required = false,
				Type = FieldType.String
			};

			const int anyRow = 1;
			const int anyColumn = 1;

			// Act
			FieldValidationResult result =
				validatorUnderTest
					.ValidateField(new Field(new DatasetUploadCellReference(anyRow, anyColumn), "Correct value type", fieldDefinition));


			// Assert
			result
				.Should().BeNull();
		}
	}
}