﻿using System.Collections.Generic;
using System.Drawing;
using System.IO;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Extension;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using Serilog;

namespace CalculateFunding.Services.Datasets.Validators
{
	[TestClass]
	public class DatasetItemValidatorTests
	{
		private static readonly IList<ProviderSummary> ProviderSummaries = MockData.CreateProviderSummaries();

		private Mock<ILogger> _logger;
		private Mock<IBlobClient> _blobClient;

		[TestInitialize]
		public void Initialize()
        {
			_logger = new Mock<ILogger>();
			_blobClient = new Mock<IBlobClient>();
		}

		[TestMethod]
		public void Validate_GivenAValidFile_ShouldNotCreateErrorSheetAndDoAnyColoringInFirstSheet()
		{
			string blobFileName = "FactorsValid.xlsx";
			string currentBlobFilePath = GetTestItemPath(blobFileName);

			using MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(currentBlobFilePath));

			AndBlobClient(currentBlobFilePath, memoryStream);

			ValidationShouldBeValid(
				"Factors.json",
				blobFileName, 
				new Dictionary<CellReference, FieldValidationResult.ReasonForFailure>(), 
				new List<string>(), 
				true,
				currentBlobFilePath,
				DatasetEmptyFieldEvaluationOption.NA);
		}

		[TestMethod]
		public void Validate_GivenAnInvalidFileWithMissingRequiredFieldForPartialFieldSetWithEvaluateAsNull_ShouldCreateErrorSheet()
		{
			// Arrange
			List<string> expectedHeaderErrors = new List<string>
			{
				"1819 Retention Factor"
			};

			string currentDatasetBlobFileName = "FactorsValid.xlsx";
			string currentDatasetBlobFilePath = GetTestItemPath(currentDatasetBlobFileName);

			using MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(currentDatasetBlobFilePath));

			AndBlobClient(currentDatasetBlobFileName, memoryStream);

			// Act & Assert
			ValidationShouldBeValid(
				"Factors.json",
				"FactorsVariousInvalidFields_PartialSet_Current_MissingRequiredield.xlsx",
				new Dictionary<CellReference, FieldValidationResult.ReasonForFailure>(),
				expectedHeaderErrors,
				false,
				currentDatasetBlobFileName,
				DatasetEmptyFieldEvaluationOption.AsNull);
		}

		[TestMethod]
		public void Validate_GivenAnInvalidFullSetFile_ShouldCreateErrorSheet()
		{
			// Arrange
			List<string> expectedHeaderErrors = new List<string>
			{
				"Local Authority"
			};

			var expectedErrors = new Dictionary<CellReference, FieldValidationResult.ReasonForFailure>()
			{
				{new CellReference(2, 1), FieldValidationResult.ReasonForFailure.ProviderIdValueMissing},
				{new CellReference(2, 5), FieldValidationResult.ReasonForFailure.DataTypeMismatch},
				{new CellReference(2, 6), FieldValidationResult.ReasonForFailure.DataTypeMismatch},

				{new CellReference(3, 1), FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider},
				{new CellReference(3, 7), FieldValidationResult.ReasonForFailure.DataTypeMismatch},

				{new CellReference(4, 7), FieldValidationResult.ReasonForFailure.DataTypeMismatch},

				{new CellReference(5, 1), FieldValidationResult.ReasonForFailure.DuplicateEntriesInTheProviderIdColumn},
				{new CellReference(5, 5), FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded},
				{new CellReference(6, 5), FieldValidationResult.ReasonForFailure.MaxOrMinValueExceeded},

				{new CellReference(7, 1), FieldValidationResult.ReasonForFailure.DuplicateEntriesInTheProviderIdColumn}
			};

			string currentDatasetBlobFileName = "FactorsVariousInvalidFields_Current.xlsx";
			string currentDatasetBlobFilePath = GetTestItemPath(currentDatasetBlobFileName);

			using MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(currentDatasetBlobFilePath));

			AndBlobClient(currentDatasetBlobFileName, memoryStream);

			// Act & Assert
			ValidationShouldBeValid(
				"Factors.json", 
				"FactorsVariousInvalidFields.xlsx", 
				expectedErrors, 
				expectedHeaderErrors, 
				false,
				currentDatasetBlobFileName,
				DatasetEmptyFieldEvaluationOption.NA);
		}

		[TestMethod]
		public void Validate_GivenAnInvalidFullSetFileForDoNotValidateProviderDatasetDefinition_ShouldCreateErrorSheet()
		{
			// Arrange
			List<string> expectedHeaderErrors = new List<string>();

			var expectedErrors = new Dictionary<CellReference, FieldValidationResult.ReasonForFailure>()
			{
				{new CellReference(2, 1), FieldValidationResult.ReasonForFailure.DataTypeMismatch},

				{new CellReference(3, 1), FieldValidationResult.ReasonForFailure.ProviderIdNotInCorrectFormat},
			};

			string currentDatasetBlobFileName = "Factors_DoNotValidateProviders_VariousInvalidFields.xlsx";
			string currentDatasetBlobFilePath = GetTestItemPath(currentDatasetBlobFileName);

			using MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(currentDatasetBlobFilePath));

			AndBlobClient(currentDatasetBlobFileName, memoryStream);

			// Act & Assert
			ValidationShouldBeValid(
				"Factors_DoNotValidateProviders.json",
				"Factors_DoNotValidateProviders_VariousInvalidFields.xlsx",
				expectedErrors,
				expectedHeaderErrors,
				false,
				currentDatasetBlobFileName,
				DatasetEmptyFieldEvaluationOption.NA);
		}

		[TestMethod]
		public void Validate_GivenAnInvalidPartialSetFile_ShouldCreateErrorSheet()
		{
			// Arrange
			List<string> expectedHeaderErrors = new List<string>()
			{
				"Local Authority",
				"1819 Area Cost Factor"
			};

			var expectedErrors = new Dictionary<CellReference, FieldValidationResult.ReasonForFailure>()
			{
				{new CellReference(2, 1), FieldValidationResult.ReasonForFailure.NewProviderMissingAllDataSchemaFields},
			};

			string blobFileName = "FactorsVariousInvalidFields_Current.xlsx";
			string currentBlobFilePath = GetTestItemPath(blobFileName);

			using MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(currentBlobFilePath));

			AndBlobClient(blobFileName, memoryStream);

			// Act & Assert
			ValidationShouldBeValid(
				"Factors.json", 
				"FactorsVariousInvalidPartialFields.xlsx", 
				expectedErrors, 
				expectedHeaderErrors, 
				false, 
				blobFileName,
				DatasetEmptyFieldEvaluationOption.NA);
		}

		private void AndBlobClient(string blobFileName, Stream stream)
		{
			Mock<ICloudBlob> mockCloudBlob = new Mock<ICloudBlob>();

			_blobClient
				.Setup(_ => _.GetBlobReferenceFromServerAsync(blobFileName))
				.ReturnsAsync(mockCloudBlob.Object);

			_blobClient
				.Setup(_ => _.DownloadToStreamAsync(mockCloudBlob.Object))
				.ReturnsAsync(stream);
		}

		private void ValidationShouldBeValid(
			string datasetDefinitionName, 
			string excelFileToTest, 
			Dictionary<CellReference, FieldValidationResult.ReasonForFailure> expectedErrors, 
			List<string> expectedHeaderErrors, 
			bool expectedValidationResult,
			string latestBlobFileName,
			DatasetEmptyFieldEvaluationOption datasetEmptyFieldEvaluationOption)
		{
			DatasetUploadValidationModelValidator datasetItemValidatorUnderTest = 
				new DatasetUploadValidationModelValidator(new ExcelDatasetReader(), _blobClient.Object, _logger.Object);
			DatasetDefinition datasetDefinition = GetDatasetDefinitionByName(datasetDefinitionName);
			FileInfo fileInfoForXlsxFile = GetFileInfoForXlsxFile(excelFileToTest);

            using ExcelPackage excelPackage = new ExcelPackage(fileInfoForXlsxFile);
            DatasetUploadValidationModel uploadValidationModel =
                new DatasetUploadValidationModel(
					excelPackage, 
					() => ProviderSummaries, 
					datasetDefinition,
					datasetEmptyFieldEvaluationOption,
					latestBlobFileName);

            var validationResult = datasetItemValidatorUnderTest.Validate(uploadValidationModel);

            validationResult.IsValid.Should().Be(expectedValidationResult);
            CheckFieldsAreColored(excelPackage.Workbook.Worksheets[1], expectedErrors);
            if (!expectedValidationResult)
            {
                CheckHeaderErrors(excelPackage.Workbook.Worksheets[2], expectedHeaderErrors, new CellReference(12, 1));
            }
        }

		private static void CheckFieldsAreColored(ExcelWorksheet excelWorksheet,
			IDictionary<CellReference, FieldValidationResult.ReasonForFailure> expectedErrors)
		{
			for (int row = 1; row <= excelWorksheet.Dimension.Rows; row++)
			{
				for (int column = 1; column <= excelWorksheet.Dimension.Columns; column++)
				{
					ExcelRange excelCell = excelWorksheet.Cells[row, column];
					bool dictionaryContainsKey = expectedErrors.TryGetValue(new CellReference(row, column),
						out FieldValidationResult.ReasonForFailure reasonForFailure);
					if (dictionaryContainsKey)
					{
						excelCell.Style.Fill.BackgroundColor.Rgb
                            .Should().BeEquivalentTo(ToAsciRgbRepresentation(reasonForFailure.GetColorCodeForFailure()));
					}
					else
					{
						excelCell.Style.Fill.BackgroundColor.Rgb.Should().BeNull();
					}
				}
			}
		}

		private static void CheckHeaderErrors(ExcelWorksheet headerErrorsWorksheet, IList<string> headerErrors,
			CellReference startingPointOfHeaders)
		{
			int headerErrorsCount = headerErrors.Count;
			for (int row = startingPointOfHeaders.Row + 1, index = 0;
				row <= startingPointOfHeaders.Row + headerErrorsCount;
				row++, index++)
			{
				ExcelRange cell = headerErrorsWorksheet.Cells[row, 1];
				cell.Value.Should().BeEquivalentTo(headerErrors[index]);
			}

			int endingRow = startingPointOfHeaders.Row + headerErrorsCount + 1;
			for (int rowIndex = endingRow; rowIndex <= headerErrorsWorksheet.Dimension.Rows; rowIndex++)
			{
				ExcelRange cell = headerErrorsWorksheet.Cells[rowIndex, 1];
				cell.Value.Should().BeNull();
			}
		}

		private static FileInfo GetFileInfoForXlsxFile(string fileName)
		{
			return new FileInfo(GetTestItemPath(fileName));
		}

		private static string GetTestItemPath(string fileName)
        {
			return Path.Join("TestItems", fileName);
		}

		private static DatasetDefinition GetDatasetDefinitionByName(string datasetDefinitionName)
		{
			JObject obj1 =
				JObject.Parse(File.ReadAllText($"DatasetDefinitions{Path.DirectorySeparatorChar}{datasetDefinitionName}"));

			DatasetDefinition datasetDefinition = obj1.ToObject<DatasetDefinition>();

			return datasetDefinition;
		}

		private static string ToAsciRgbRepresentation(Color color)
		{
			return $"FF{color.R:X2}{color.G:X2}{color.B:X2}";
		}

		private class CellReference
		{
			public CellReference(int row, int column)
			{
				Row = row;
				Column = column;
			}

			public int Row { get; }

			public int Column { get; }

			private bool Equals(CellReference other)
			{
				return Row == other.Row && Column == other.Column;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((CellReference) obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Row * 397) ^ Column;
				}
			}
		}
	}
}