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

        [TestMethod]
        [DataRow(FieldType.Boolean, true)]
        [DataRow(FieldType.Char, 'c')]
        [DataRow(FieldType.Byte, 10)]
        [DataRow(FieldType.Integer, 1)]
        [DataRow(FieldType.Float, 3.5F)]
        [DataRow(FieldType.Decimal, 10.4)]
        [DataRow(FieldType.NullableOfDecimal, 1.2)]
        [DataRow(FieldType.NullableOfDecimal, null)]
        [DataRow(FieldType.NullableOfDecimal, "")]
        [DataRow(FieldType.NullableOfDecimal, "NULL")]
        [DataRow(FieldType.NullableOfInteger, 1)]
        [DataRow(FieldType.NullableOfInteger, null)]
        [DataRow(FieldType.NullableOfInteger, "")]
        [DataRow(FieldType.NullableOfInteger, "NULL")]
        [DataRow(FieldType.DateTime, "01/01/12")]
        [DataRow(FieldType.String, "Whatever")]
        public void ValidateField_WhenFieldValueIsCorrectType_ShouldReturnNull(FieldType fieldType, object value)
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
                Type = fieldType
            };

            const int anyRow = 1;
            const int anyColumn = 1;

            // Act
            FieldValidationResult result =
                validatorUnderTest
                    .ValidateField(new Field(new DatasetUploadCellReference(anyRow, anyColumn), value, fieldDefinition));

            // Assert
            result
                .Should()
                .BeNull();
        }

        [TestMethod]
        [DataRow(FieldType.Boolean, 99)]
        [DataRow(FieldType.Char, "anything")]
        [DataRow(FieldType.Byte, "anything")]
        [DataRow(FieldType.Integer, 1.0999)]
        [DataRow(FieldType.Float, "test")]
        [DataRow(FieldType.Decimal, "anything")]
        [DataRow(FieldType.DateTime, "not a date")]
        [DataRow(FieldType.NullableOfDecimal, "anything")]
        [DataRow(FieldType.NullableOfInteger, "anything")]
        public void ValidateField_WhenFieldValueIsNotCorrectType_ShouldReturnError(FieldType fieldType, object value)
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
                Type = fieldType
            };

            const int anyRow = 1;
            const int anyColumn = 1;

            // Act
            FieldValidationResult result =
                validatorUnderTest
                    .ValidateField(new Field(new DatasetUploadCellReference(anyRow, anyColumn), value, fieldDefinition));

            // Assert
            result
                .Should()
                .NotBeNull();
        }
    }
}