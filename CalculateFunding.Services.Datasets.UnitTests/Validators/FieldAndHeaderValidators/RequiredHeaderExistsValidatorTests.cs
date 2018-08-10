using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
	[TestClass]
    public class RequiredHeaderExistsValidatorTests
    {
		private const string RequiredHeaderName = "Rid";
	    private const string RequiredHeaderRid = "Parent Rid";
	    private const string RequiredHeaderUpin = "UPIN";

	    [TestMethod]
	    public void ValidateHeaders_WhenSomeHeadersAreNotAvailable_ShouldReturnCorrectValidationResult()
		{
			// Arrange
			IList<FieldDefinition> fieldDefinitionsTestData = CreateFieldDefinitions();
			IList<HeaderField> headerFields = new List<HeaderField>()
			{
				new HeaderField(RequiredHeaderName)
			};
			RequiredHeaderExistsValidator requiredHeaderExistsValidator = new RequiredHeaderExistsValidator(fieldDefinitionsTestData);

			// Act
			IList<HeaderValidationResult> invalidResults = requiredHeaderExistsValidator.ValidateHeaders(headerFields);

			// Assert
			invalidResults
				.Count
				.ShouldBeEquivalentTo(2);

			invalidResults
				.Should()
				.Contain(vr => vr.FieldDefinitionValidated.Name == RequiredHeaderRid);

			invalidResults
				.Should()
				.Contain(vr => vr.FieldDefinitionValidated.Name == RequiredHeaderUpin);
		}

	    [TestMethod]
	    public void ValidateHeaders_WhenAllHeadersAreAvailable_ShouldReturnCorrectValidationResult()
	    {
		    // Arrange
		    IList<FieldDefinition> fieldDefinitionsTestData = CreateFieldDefinitions();
		    IList<HeaderField> headerFields = new List<HeaderField>()
		    {
			    new HeaderField(RequiredHeaderName),
			    new HeaderField(RequiredHeaderRid),
			    new HeaderField(RequiredHeaderUpin)
		    };
		    RequiredHeaderExistsValidator requiredHeaderExistsValidator = new RequiredHeaderExistsValidator(fieldDefinitionsTestData);

		    // Act
		    IList<HeaderValidationResult> invalidResults = requiredHeaderExistsValidator.ValidateHeaders(headerFields);

		    // Assert
		    invalidResults
			    .Should().BeEmpty();
	    }

		private static IList<FieldDefinition> CreateFieldDefinitions()
	    {
			List<FieldDefinition> fieldDefinitions = new List<FieldDefinition>
			{
				new FieldDefinition
				{
					Description = "Rid is the unique reference from The Store",
					Id = "1100001",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = RequiredHeaderName,
					Required = false,
					Type = FieldType.String
				},
				new FieldDefinition
				{
					Description = "The Rid of the parent provider (from The Store)",
					Id = "1100002",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = RequiredHeaderRid,
					Required = false,
					Type = FieldType.String
				},
				new FieldDefinition
				{
					Description = "The UPIN identifier for the provider",
					Id = "1100003",
					IdentifierFieldType = IdentifierFieldType.UPIN,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = RequiredHeaderUpin,
					Required = false,
					Type = FieldType.String
				}
			};
		    return fieldDefinitions;
	    }
	}
}
