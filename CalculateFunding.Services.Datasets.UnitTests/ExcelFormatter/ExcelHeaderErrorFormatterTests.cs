using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.ExcelFormatter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OfficeOpenXml;

namespace CalculateFunding.Services.Datasets.ExcelFormatter
{
	[TestClass]
	public class ExcelHeaderErrorFormatterTests
	{
		[TestMethod]
		public void FormatExcelSheetBasedOnErrors_GivenSomeMissingHeaders_ShouldFillInWorksheetWithTheErrors()
		{
            using ExcelPackage excelPackage = new ExcelPackage();
            // Arrange
            FieldDefinition fieldDefinitionParentRid = new FieldDefinition
            {
                Description = "The Rid of the parent provider (from The Store)",
                Id = "1100002",
                IdentifierFieldType = null,
                MatchExpression = null,
                Maximum = null,
                Minimum = null,
                Name = "Parent Rid",
                Required = false,
                Type = FieldType.String
            };

            FieldDefinition fieldDefinitionRid = new FieldDefinition
            {
                Description = "Rid is the unique reference from The Store",
                Id = "1100001",
                IdentifierFieldType = null,
                MatchExpression = null,
                Maximum = null,
                Minimum = null,
                Name = "Rid",
                Required = false,
                Type = FieldType.String
            };

            List<HeaderValidationResult> headerValidationResultsToReturn = new List<HeaderValidationResult>
                {
                    new HeaderValidationResult(fieldDefinitionParentRid),
                    new HeaderValidationResult(fieldDefinitionRid)
                };

            IDatasetUploadValidationResult mockUploadValidationResult = Substitute.For<IDatasetUploadValidationResult>();
            mockUploadValidationResult.HeaderValitionFailures.Returns(headerValidationResultsToReturn);
            mockUploadValidationResult.FieldValidationFailures.Returns(new List<FieldValidationResult>());

            // Act
            ExcelHeaderErrorFormatter formatterUnderTest = new ExcelHeaderErrorFormatter(excelPackage);
            formatterUnderTest.FormatExcelSheetBasedOnErrors(mockUploadValidationResult);

            // Assert
            ExcelWorksheet errorsWorkSheet = excelPackage.Workbook.Worksheets["Errors"];
            errorsWorkSheet.Should().NotBeNull();

            errorsWorkSheet.Cells[13, 1].Value.Should().BeEquivalentTo(fieldDefinitionParentRid.Name);
            errorsWorkSheet.Cells[14, 1].Value.Should().BeEquivalentTo(fieldDefinitionRid.Name);

            for (int rowIndex = 15; rowIndex < 25; rowIndex++)
            {
                errorsWorkSheet.Cells[rowIndex, 1].Value.Should().BeNull();
            }
        }

		[TestMethod]
		public void FormatExcelSheetBasedOnErrors_GivenNoErrorInResult_ShouldNotCreateAdditionalWorksheet()
		{
			using (var excelPackage = new ExcelPackage())
			{
				// Arrange
				IDatasetUploadValidationResult mockUploadValidationResult = Substitute.For<IDatasetUploadValidationResult>();
				mockUploadValidationResult.HeaderValitionFailures.Returns(new List<HeaderValidationResult>());
				mockUploadValidationResult.FieldValidationFailures.Returns(new List<FieldValidationResult>());
				mockUploadValidationResult.IsValid().Returns(true);

				// Act
				ExcelHeaderErrorFormatter formatterUnderTest = new ExcelHeaderErrorFormatter(excelPackage);
				formatterUnderTest.FormatExcelSheetBasedOnErrors(mockUploadValidationResult);

				// Assert
				ExcelWorksheet errorsWorkSheet = excelPackage.Workbook.Worksheets["Errors"];
				errorsWorkSheet.Should().BeNull();
			}
		}
	}
}
