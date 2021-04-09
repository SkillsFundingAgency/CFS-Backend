using System;
using System.Drawing;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.ExcelFormatter;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Validators.FieldAndHeaderValidators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CalculateFunding.Services.Datasets.ExcelFormatter
{
    [TestClass]
    public class ExcelHeaderErrorFormatterTests
    {
        private ExcelPackage _excelPackage;
        private DatasetUploadValidationResult _validationResult;

        private ExcelHeaderErrorFormatter _formatter;

        [TestInitialize]
        public void SetUp()
        {
            _excelPackage = new ExcelPackage();
            _validationResult = new DatasetUploadValidationResult();

            _formatter = new ExcelHeaderErrorFormatter(_excelPackage);
        }

        [TestMethod]
        public void FormatsBackgroundColourForHeaderValidationErrorsWithKeyColour()
        {
            Color duplicateColumnNameColour = ReasonForFailureColour.LightBlue;
            
            string headerOne = NewRandomString();
            string headerTwo = NewRandomString();
            string headerThree = NewRandomString();
            
            FieldDefinition fieldDefinitionTwo = NewFieldDefinition(_ => _.WithName(headerTwo));
            
            GivenTheHeaderValidationResults(NewHeaderValidationResult(_ => _.WithFieldDefinition(fieldDefinitionTwo)
                .WithReasonForFailure(DatasetCellReasonForFailure.DuplicateColumnHeader)
                .WithHasBackgroundKeyColour(true)));
            AndTheExcelWorksheetHasTheHeaders(headerOne, headerTwo, headerThree, headerTwo);
            
            WhenTheHeaderResultsAreFormatted();
            
            ThenTheCellsShouldHaveTheBackgroundColour((2, duplicateColumnNameColour),
                (4, duplicateColumnNameColour));
        }
        
        [TestMethod]
        public void FormatExcelSheetBasedOnErrors_GivenSomeMissingHeaders_ShouldFillInWorksheetWithTheErrors()
        {
            FieldDefinition fieldDefinitionParentRid = NewFieldDefinition();
            FieldDefinition fieldDefinitionRid = NewFieldDefinition();

            GivenTheHeaderValidationResults(NewHeaderValidationResult(_ => _.WithFieldDefinition(fieldDefinitionParentRid)),
                NewHeaderValidationResult(_ => _.WithFieldDefinition(fieldDefinitionRid)));
            
            WhenTheHeaderResultsAreFormatted();
            
            ExcelWorksheet errorsWorkSheet = _excelPackage.Workbook.Worksheets["Errors"];
            errorsWorkSheet
                .Should()
                .NotBeNull();

            errorsWorkSheet.Cells[14, 1]
                .Value
                .Should()
                .Be(fieldDefinitionParentRid.Name);
            errorsWorkSheet.Cells[15, 1]
                .Value
                .Should()
                .Be(fieldDefinitionRid.Name);

            for (int rowIndex = 16; rowIndex < 26; rowIndex++)
            {
                errorsWorkSheet.Cells[rowIndex, 1]
                    .Value
                    .Should()
                    .BeNull();
            }
        }

        [TestMethod]
        public void FormatExcelSheetBasedOnErrors_GivenNoErrorInResult_ShouldNotCreateAdditionalWorksheet()
        {
            WhenTheHeaderResultsAreFormatted();

            ExcelWorksheet errorsWorkSheet = _excelPackage.Workbook.Worksheets["Errors"];
            
            errorsWorkSheet
                .Should()
                .BeNull();
        }

        private void GivenTheHeaderValidationResults(params HeaderValidationResult[] validationResults)
            => _validationResult.HeaderValidationFailures = validationResults;

        private void AndTheExcelWorksheetHasTheHeaders(params string[] headers)
        {
            ExcelWorksheet workbookWorksheet = _excelPackage
                .Workbook
                .Worksheets
                .Add("Sheet1");
            
            workbookWorksheet.InsertRow(1, 1);
            workbookWorksheet.InsertColumn(1, headers.Length);

            for (int column = 1; column <= headers.Length; column++)
            {
                workbookWorksheet.Cells[1, column].Value = headers[column - 1];
            }
        }

        private void ThenTheCellsShouldHaveTheBackgroundColour(params (int column, Color colour)[] expectedFormattedCells)
        {
            ExcelWorksheet worksheet = _excelPackage.Workbook.Worksheets[1];
            
            foreach ((int column, Color colour) expectedFormattedCell in expectedFormattedCells)
            {
                ExcelFill style = worksheet?.Cells[1, expectedFormattedCell.column]?.Style.Fill;

                style
                    .Should()
                    .NotBeNull();

                style.PatternType
                    .Should()
                    .Be(ExcelFillStyle.Solid);

                Color colour = expectedFormattedCell.colour;
                
                style.BackgroundColor
                    .Rgb
                    .Should()
                    .Be($"FF{colour.R:X2}{colour.G:X2}{colour.B:X2}");
            }
        }

        private void WhenTheHeaderResultsAreFormatted()
            => _formatter.FormatExcelSheetBasedOnErrors(_validationResult);

        private static HeaderValidationResult NewHeaderValidationResult(Action<HeaderValidationResultBuilder> setUp = null)
        {
            HeaderValidationResultBuilder headerValidationResultBuilder = new HeaderValidationResultBuilder();

            setUp?.Invoke(headerValidationResultBuilder);
            
            return headerValidationResultBuilder.Build();
        }

        private static FieldDefinition NewFieldDefinition(Action<FieldDefinitionBuilder> setUp = null)
        {
            FieldDefinitionBuilder fieldDefinitionBuilder = new FieldDefinitionBuilder();

            setUp?.Invoke(fieldDefinitionBuilder);

            return fieldDefinitionBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}