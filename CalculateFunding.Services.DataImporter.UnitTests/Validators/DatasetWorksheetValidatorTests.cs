using CalculateFunding.Services.DataImporter.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using System.Linq;

namespace CalculateFunding.Services.Scenarios.Validators
{
    [TestClass]
    public class DatasetWorksheetValidatorTests
    {
        [TestMethod]
        public void Validate_GivenWorksheetWithNoData_ValidIsFalse()
        {
            //Arrange
            ExcelPackage package = new ExcelPackage();
            
            ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("Test Worksheet");

            DatasetWorksheetValidator validator = new DatasetWorksheetValidator();

            //Act
            ValidationResult result = validator.Validate(package);

            //Assert
            result
                .IsValid
                .Should()
                .Be(false);
        }

        [TestMethod]
        public void Validate_GivenWorksheetWithMergedCells_ValidIsFalse()
        {
            //Arrange
            ExcelPackage package = new ExcelPackage();
            
            ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("Test Worksheet");

            workSheet.Cells["A1"].Value = "1";
            workSheet.Cells["B1"].Value = "2";
            workSheet.Cells["C1"].Value = "3";
            workSheet.Cells["D1"].Value = "1";
            workSheet.Cells["E1"].Value = "2";
            workSheet.Cells["F1"].Value = "3";
            workSheet.Cells["B1:C1"].Merge = true;
            workSheet.Cells["D1:E1"].Merge = true;

            DatasetWorksheetValidator validator = new DatasetWorksheetValidator();

            //Act
            ValidationResult result = validator.Validate(package);

            //Assert
            result
                .IsValid
                .Should()
                .Be(false);

            result
                .Errors
                .First()
                .ErrorMessage
                .Should()
                .Be("Invalid worksheet, worksheet cannot contain merged cells (B1:C1,D1:E1)");
        }

        [TestMethod]
        public void Validate_GivenValidWorksheet_ValidIsTrue()
        {
            //Arrange
            ExcelPackage package = new ExcelPackage();
            
            ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("Test Worksheet");

            workSheet.Cells["A1"].Value = "1";
            workSheet.Cells["B1"].Value = "2";
            workSheet.Cells["C1"].Value = "3";

            DatasetWorksheetValidator validator = new DatasetWorksheetValidator();

            //Act
            ValidationResult result = validator.Validate(package);

            //Assert
            result
                .IsValid
                .Should()
                .Be(true);
        }
    }
}
