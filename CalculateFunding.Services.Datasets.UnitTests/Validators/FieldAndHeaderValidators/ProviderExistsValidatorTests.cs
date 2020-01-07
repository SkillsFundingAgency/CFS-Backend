using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators
{
	[TestClass]
    public class ProviderExistsValidatorTests
    {
	    [TestMethod]
	    public void Validate_WhenFieldProviderExists_ShouldReturnCorrectValidationResult()
	    {
		    // Arrange
		    IList<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();
		    var providerExistsValidator = new ProviderExistsValidator(providerSummaries);

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


		    Field field = new Field(new DatasetUploadCellReference(0, 2), "107013", definition);

		    // Act
		    FieldValidationResult result = providerExistsValidator.ValidateField(field);

		    // Assert
		    result.Should().BeNull();
	    }

        [TestMethod]
        public void Validate_WhenFieldProviderExistsWithLaCode_ShouldReturnCorrectValidationResult()
        {
            // Arrange
            IList<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();
            var providerExistsValidator = new ProviderExistsValidator(providerSummaries);

            FieldDefinition definition = new FieldDefinition
            {
                Name = "LACode",
                Description = "The LaCode identifier for the provider",
                Id = "12345",
                IdentifierFieldType = IdentifierFieldType.LACode,
                MatchExpression = null,
                Maximum = null,
                Minimum = null,
                Required = false,
                Type = FieldType.String
            };


            Field field = new Field(new DatasetUploadCellReference(0, 2), "12345", definition);

            // Act
            FieldValidationResult result = providerExistsValidator.ValidateField(field);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void Validate_WhenFieldProviderDoesNotExistWithLaCode_ShouldReturnCorrectValidationResult()
        {
            // Arrange
            IList<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();
            var providerExistsValidator = new ProviderExistsValidator(providerSummaries);

            FieldDefinition definition = new FieldDefinition
            {
                Name = "LACode",
                Description = "The LaCode identifier for the provider",
                Id = "12345",
                IdentifierFieldType = IdentifierFieldType.LACode,
                MatchExpression = null,
                Maximum = null,
                Minimum = null,
                Required = false,
                Type = FieldType.String
            };


            Field field = new Field(new DatasetUploadCellReference(0, 2), "12345888", definition);

            // Act
            FieldValidationResult result = providerExistsValidator.ValidateField(field);

            // Assert
            result.FieldValidated
                .Should().Be(field);

            result.ReasonOfFailure
                .Should().Be(FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider);
        }

        [TestMethod]
	    public void Validate_WhenFieldProviderDoesNotExist_ShouldReturnCorrectValidationResult()
	    {
		    // Arrange
		    IList<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();
		    var providerExistsValidator = new ProviderExistsValidator(providerSummaries);

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


		    Field field = new Field(new DatasetUploadCellReference(0, 2), "107019", definition);

		    // Act
		    FieldValidationResult result = providerExistsValidator.ValidateField(field);

		    // Assert
		    result.FieldValidated
			    .Should().Be(field);

		    result.ReasonOfFailure
			    .Should().Be(FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider);
	    }

	    [TestMethod]
	    public void Validate_WhenIdentifierFieldIsNull_ShouldReturnCorrectValidationResult()
	    {
		    // Arrange
		    IList<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();
		    var providerExistsValidator = new ProviderExistsValidator(providerSummaries);

		    FieldDefinition definition = new FieldDefinition
		    {
			    Name = "UPIN",
			    Description = "The UPIN identifier for the provider",
			    Id = "1100003",
			    IdentifierFieldType = null,
			    MatchExpression = null,
			    Maximum = null,
			    Minimum = null,
			    Required = false,
			    Type = FieldType.String
		    };


		    Field field = new Field(new DatasetUploadCellReference(0, 2), "107013", definition);

		    // Act
		    FieldValidationResult result = providerExistsValidator.ValidateField(field);

		    // Assert
		    result.Should().BeNull();
	    }

		private IDictionary<string, ProviderSummary> CreateProviderSummaries()
		{
			var provSummariesToExport = new List<ProviderSummary>
			{
				new ProviderSummary
				{
					Authority = "Barnsley",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "8001",
					Id = null,
					LACode = "12345",
					LegalName = null,
					Name = "Barnsley College",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "General FE and Tertiary",
					ProviderType = "16-18 Provider",
					UKPRN = null,
					UPIN = "107013",
					URN = "130524"
				},
				new ProviderSummary
				{
					Authority = "Barnsley",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "0",
					Id = null,
					LACode = null,
					LegalName = null,
					Name = "Independent Training Services Limited",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "Independent Private Provider",
					ProviderType = "16-18 Provider",
					UKPRN = null,
					UPIN = "107016",
					URN = "0"
				},
				new ProviderSummary
				{
					Authority = "Surrey",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "8600",
					Id = null,
					LACode = null,
					LegalName = null,
					Name = "Godalming College",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "Academies",
					ProviderType = "Academy",
					UKPRN = null,
					UPIN = "139305",
					URN = "145004"
				},
				new ProviderSummary
				{
					Authority = "Bath and North East Somerset",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "8009",
					Id = null,
					LACode = null,
					LegalName = null,
					Name = "Bath College",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "General FE and Tertiary",
					ProviderType = "16-18 Provider",
					UKPRN = null,
					UPIN = "105154",
					URN = "130558"
				},
				new ProviderSummary
				{
					Authority = "Bath and North East Somerset",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "5400",
					Id = null,
					LACode = null,
					LegalName = null,
					Name = "Beechen Cliff School",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "Academies",
					ProviderType = "Academy",
					UKPRN = null,
					UPIN = "119570",
					URN = "136520"
				},
				new ProviderSummary
				{
					Authority = "Bath and North East Somerset",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "4130",
					Id = null,
					LACode = null,
					LegalName = null,
					Name = "Chew Valley School",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "School Sixth Form",
					ProviderType = "School Sixth Form",
					UKPRN = null,
					UPIN = "114496",
					URN = "109306"
				},
				new ProviderSummary
				{
					Authority = "Bath and North East Somerset",
					CrmAccountId = null,
					DateOpened = null,
					EstablishmentNumber = "4107",
					Id = null,
					LACode = null,
					LegalName = null,
					Name = "Hayesfield Girls School",
					NavVendorNo = null,
					ProviderProfileIdType = null,
					ProviderSubType = "Academies",
					ProviderType = "Academy",
					UKPRN = null,
					UPIN = "119966",
					URN = "136966"
				}
			};
			return provSummariesToExport.ToDictionary(p => p.Name);
		}
	}
}
