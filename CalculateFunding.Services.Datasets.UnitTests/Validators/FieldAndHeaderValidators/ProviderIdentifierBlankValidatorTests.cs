using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
	[TestClass]
    public class ProviderIdentifierBlankValidatorTests
    {
		private static readonly DatasetUploadCellReference AnyCellReference = new DatasetUploadCellReference(1, 1);
		[TestMethod]
	    public void ValidateField_GivenProviderIdentifierIsNotBlank_ShouldReturnCorrectResult()
	    {
		    // Arrange
		    ProviderIdentifierBlankValidator validatorUnderTest = new ProviderIdentifierBlankValidator();
		    DatasetUploadCellReference anyCellReference = new DatasetUploadCellReference(0, 2);

			FieldDefinition definition = new FieldDefinition
		    {
			    Name = "UPIN",
			    Description = "The UPIN identifier for the provider",
			    Id = "1100003",
			    IdentifierFieldType = IdentifierFieldType.UPIN,
			    MatchExpression = null,
			    Maximum = null,
			    Minimum = null,
			    Required = false,
			    Type = FieldType.String
		    };

		    Field field = new Field(anyCellReference, "107013", definition);

		    // Act
		    FieldValidationResult result = validatorUnderTest.ValidateField(field);

		    // Assert
		    result.Should().BeNull();
	    }

	    [TestMethod]
	    public void ValidateField_GivenProviderIdentifierIsBlank_ShouldReturnCorrectResult()
	    {
		    // Arrange
		    ProviderIdentifierBlankValidator validatorUnderTest = new ProviderIdentifierBlankValidator();

		    FieldDefinition definition = new FieldDefinition
		    {
			    Name = "UPIN",
			    Description = "The UPIN identifier for the provider",
			    Id = "1100003",
			    IdentifierFieldType = IdentifierFieldType.UPIN,
			    MatchExpression = null,
			    Maximum = null,
			    Minimum = null,
			    Required = false,
			    Type = FieldType.String
		    };

		    Field field = new Field(AnyCellReference, string.Empty, definition);

		    // Act
		    FieldValidationResult result = validatorUnderTest.ValidateField(field);

		    // Assert
		    result.Should().NotBeNull();
		    result.ReasonOfFailure
			    .Should().Be(DatasetCellReasonForFailure.ProviderIdValueMissing);
	    }

	    [TestMethod]
	    public void ValidateField_GivenAFieldThatIsNotAProviderIdentifier_ShouldIgnoreAndReturnCorrectResult()
	    {
			// Arrange
		    const string someOtherField = "some other field";
			ProviderIdentifierBlankValidator validatorUnderTest = new ProviderIdentifierBlankValidator();

			FieldDefinition fieldDefinitionForSomeOtherField = new FieldDefinition
		    {
			    Description = "Some other field",
			    Id = "1100003",
			    IdentifierFieldType = null,
			    MatchExpression = null,
			    Maximum = null,
			    Minimum = null,
			    Name = someOtherField,
			    Required = false,
			    Type = FieldType.String
		    };

		    Field field = new Field(AnyCellReference, string.Empty, fieldDefinitionForSomeOtherField);

		    // Act
		    FieldValidationResult result = validatorUnderTest.ValidateField(field);

		    // Assert
		    result.Should().BeNull();
	    }
    }
}
