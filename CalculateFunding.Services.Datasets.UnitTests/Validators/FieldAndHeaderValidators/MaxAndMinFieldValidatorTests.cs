using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
	[TestClass]
	public class MaxAndMinFieldValidatorTests
	{
		[TestMethod]
		public void ValidateField_WhenGivenFieldIsMax_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 10000, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void ValidateField_WhenGivenFieldIsMin_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 10000, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void ValidateField_WhenGivenFieldIsLessThanMinimum_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 8999, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.FieldValidated.CellReference.Should().Be(anyCellReference);
			result.ReasonOfFailure.Should().Be(FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded);
		}

		[TestMethod]
		public void ValidateField_WhenGivenFieldIsMoreThanMaximum_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 10001, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.FieldValidated.CellReference.Should().Be(anyCellReference);
			result.ReasonOfFailure.Should().Be(FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded);
		}

		[TestMethod]
		public void ValidateField_WhenGivenDecimalFieldIsMax_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 10000m, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void ValidateField_WhenGivenDecimalFieldIsMin_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 10000m, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.Should().BeNull();
		}

		[TestMethod]
		public void ValidateField_WhenGivenDecimalFieldIsLessThanMinimum_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 8999m, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.FieldValidated.CellReference.Should().Be(anyCellReference);
			result.ReasonOfFailure.Should().Be(FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded);
		}

		[TestMethod]
		public void ValidateField_WhenGivenDecimalFieldIsMoreThanMaximum_ShouldReturnCorrectResult()
		{
			// Arrange
			DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(1, 1);

			MaxAndMinFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

			FieldDefinition fieldDefinition = new FieldDefinition
			{
				Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
				Id = "1100013",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = 10000,
				Minimum = 9000,
				Name = "Number of students",
				Required = false,
				Type = FieldType.Integer
			};

			Field field = new Field(anyCellReference, 10001m, fieldDefinition);

			// Act
			FieldValidationResult result = maxAndMinFieldValidator.ValidateField(field);

			// Assert
			result.FieldValidated.CellReference.Should().Be(anyCellReference);
			result.ReasonOfFailure.Should().Be(FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded);
		}
	}
}