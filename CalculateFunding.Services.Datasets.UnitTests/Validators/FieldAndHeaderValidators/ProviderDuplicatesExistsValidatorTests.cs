using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
	[TestClass]
    public class ProviderDuplicatesExistsValidatorTests
	{
		private static readonly IDictionary<IdentifierFieldType, FieldDefinition> FieldDefinitions = CreateProviderIdFieldDefinitions();

		[TestMethod]
		public void ValidateAll_GivenDuplicateProvidersExists_ShouldReturnCorrectCellAndReason()
		{
			// Arrange 
			ProviderDuplicatesExistsValidator validatorUnderTest = new ProviderDuplicatesExistsValidator();

			IList<Field> fieldsBeingValidated = new List<Field>()
			{
				new Field(new DatasetUploadCellReference(2, 1), "12345678", FieldDefinitions[IdentifierFieldType.URN]),
				new Field(new DatasetUploadCellReference(3, 1), "12345678", FieldDefinitions[IdentifierFieldType.URN]),
				
				new Field(new DatasetUploadCellReference(4, 2), "1471011", FieldDefinitions[IdentifierFieldType.UPIN]),

				new Field(new DatasetUploadCellReference(5, 3), "EST225", FieldDefinitions[IdentifierFieldType.EstablishmentNumber]),
				new Field(new DatasetUploadCellReference(6, 3), "EST225", FieldDefinitions[IdentifierFieldType.EstablishmentNumber])
			};
			
			// Act
			IList<FieldValidationResult> result = validatorUnderTest.ValidateAllFields(fieldsBeingValidated);

			// Assert
			result
				.Count.ShouldBeEquivalentTo(4);
			result.All(e => e.ReasonOfFailure == FieldValidationResult.ReasonForFailure.DuplicateEntriesInTheProviderIdColumn).Should().BeTrue();

			List<FieldValidationResult> urnErrors = result.Where(r => r.FieldValidated.Value.ToString() == "12345678").ToList();
			urnErrors.Count.ShouldBeEquivalentTo(2);
			ShouldContainCellReferenceForRowAndColumn(urnErrors, 2, 1)
				.Should().BeTrue();
			ShouldContainCellReferenceForRowAndColumn(urnErrors, 3, 1)
				.Should().BeTrue();

			List<FieldValidationResult> establishmentNumberErrors = result.Where(r => r.FieldValidated.Value.ToString() == "EST225").ToList();
			ShouldContainCellReferenceForRowAndColumn(establishmentNumberErrors, 5, 3)
				.Should().BeTrue();
			ShouldContainCellReferenceForRowAndColumn(establishmentNumberErrors, 6, 3)
				.Should().BeTrue();
		}

		[TestMethod]
		public void ValidateAll_GivenDuplicateBlankValues_ShouldReturnEmptyResult()
		{
			// Arrange 
			ProviderDuplicatesExistsValidator validatorUnderTest = new ProviderDuplicatesExistsValidator();

			IList<Field> fieldsBeingValidated = new List<Field>()
			{
				new Field(new DatasetUploadCellReference(2, 1), string.Empty, FieldDefinitions[IdentifierFieldType.URN]),
				new Field(new DatasetUploadCellReference(3, 1), string.Empty, FieldDefinitions[IdentifierFieldType.URN])
			};

			// Act
			IList<FieldValidationResult> result = validatorUnderTest.ValidateAllFields(fieldsBeingValidated);

			// Assert
			result
				.Count.ShouldBeEquivalentTo(0);
		}

		[TestMethod]
		public void ValidateAll_GivenFieldsThatAreNotProviderIdentifier_ShouldReturnEmptyResult()
		{
			// Arrange 
			const string testFieldName = "TestField";
			const string duplicatedValue = "DuplicatedValue";
			ProviderDuplicatesExistsValidator validatorUnderTest = new ProviderDuplicatesExistsValidator();

			FieldDefinition fieldDefinitionForTestFieldName = new FieldDefinition
			{
				Description = "The UPIN identifier for the provider",
				Id = "1100003",
				IdentifierFieldType = null,
				MatchExpression = null,
				Maximum = null,
				Minimum = null,
				Name = testFieldName,
				Required = false,
				Type = FieldType.String
			};

			IList<Field> fieldsBeingValidated = new List<Field>()
			{
				new Field(new DatasetUploadCellReference(1, 1), duplicatedValue, fieldDefinitionForTestFieldName),
				new Field(new DatasetUploadCellReference(2, 1), duplicatedValue, fieldDefinitionForTestFieldName)
			};

			// Act
			IList<FieldValidationResult> result = validatorUnderTest.ValidateAllFields(fieldsBeingValidated);

			// Assert
			result.Count.ShouldBeEquivalentTo(0);
		}

		private static bool ShouldContainCellReferenceForRowAndColumn(List<FieldValidationResult> fieldValidationResults, int row, int column)
		{
			return
				fieldValidationResults
					.Select(ue => ue.FieldValidated.CellReference)
					.SingleOrDefault(c => c.RowIndex == row && c.ColumnIndex == column) != null;
		}

		private static Dictionary<IdentifierFieldType, FieldDefinition> CreateProviderIdFieldDefinitions()
	    {
		    return new Dictionary<IdentifierFieldType, FieldDefinition>()
		    {
			    {
				    IdentifierFieldType.UPIN,
				    new FieldDefinition
				    {
					    Description = "The UPIN identifier for the provider",
					    Id = "1100003",
					    IdentifierFieldType = IdentifierFieldType.UPIN,
					    MatchExpression = null,
					    Maximum = null,
					    Minimum = null,
					    Name = "UPIN",
					    Required = false,
					    Type = FieldType.String
				    }
			    },
			    {
				    IdentifierFieldType.URN,

				    new FieldDefinition
				    {
					    Description = "The URN identifier for the provider",
					    Id = "1100008",
					    IdentifierFieldType = IdentifierFieldType.URN,
					    MatchExpression = null,
					    Maximum = null,
					    Minimum = null,
					    Name = "URN",
					    Required = false,
					    Type = FieldType.String
				    }
			    },
			    {
				    IdentifierFieldType.EstablishmentNumber,
				    new FieldDefinition
				    {
					    Description = "The estblishment number for the provider",
					    Id = "1100009",
					    IdentifierFieldType = IdentifierFieldType.EstablishmentNumber,
					    MatchExpression = null,
					    Maximum = null,
					    Minimum = null,
					    Name = "Establishment Number",
					    Required = false,
					    Type = FieldType.String
				    }
			    }
		    };
	    }
    }
}
