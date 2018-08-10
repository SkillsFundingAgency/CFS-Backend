using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using OfficeOpenXml;

namespace CalculateFunding.Services.Datasets.Validators
{
	[TestClass]
	public class DatasetItemValidatorTests
	{
		private static readonly Field Field = new Field(new DatasetUploadCellReference(1, 2), "Doesn't matter", new FieldDefinition());
		private static readonly FieldValidationResult PassedValidationResult = null;
		private static readonly FieldValidationResult FailedValidationResult = new FieldValidationResult(Field, false, FieldValidationResult.ReasonForFailure.ProviderIdMismatchWithServiceProvider);
		private static readonly HeaderValidationResult PassedHeaderValidationResult = new HeaderValidationResult(new FieldDefinition());


		[TestMethod]
		public void Validate_WhenAllValidationPasses_ShouldReflectInValidationResult()
		{
			// Arrange
			TableLoadResult tableLoadResult = new TableLoadResult
			{
				Rows = new List<RowLoadResult>
				{
					new RowLoadResult
					{
						Fields = new Dictionary<string, object>
						{
							{"Rid", "002Ba107013"},
							{"Parent Rid", null},
						},
						Identifier = null,
						IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
					},
					new RowLoadResult
					{
						Fields = new Dictionary<string, object>
						{
							{"Rid", "002Ba107016"},
							{"Parent Rid", null},
						}
					}
				}
			};
			DatasetDefinition datasetDefinition = CreateDatasetDefinition(2);
			ExcelPackage excelPackage = new ExcelPackage();
			List<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();

			IExcelDatasetReader mockExcelDatasetReader = Substitute.For<IExcelDatasetReader>();
			mockExcelDatasetReader
				.Read(Arg.Any<ExcelPackage>(), Arg.Any<DatasetDefinition>())
				.Returns(tableLoadResult);

			IFieldValidator providerFieldValidator = Substitute.For<IFieldValidator>();
			providerFieldValidator.ValidateField(Arg.Any<Field>()).Returns(PassedValidationResult, PassedValidationResult, PassedValidationResult, PassedValidationResult);

			IHeaderValidator headerValidator = Substitute.For<IHeaderValidator>();
			headerValidator.ValidateHeaders(Arg.Any<IList<HeaderField>>()).Returns(new List<HeaderValidationResult>());

			DatasetUploadValidationModel datasetUploadValidationModel =
				new DatasetUploadValidationModel(excelPackage, () => providerSummaries, datasetDefinition, tableLoadResult);

			DatasetItemValidator validatorUnderTest = new DatasetItemValidator(mockExcelDatasetReader)
			{
				FieldValidators = new List<IFieldValidator>() { providerFieldValidator },
				HeaderValidator = headerValidator
			};

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(datasetUploadValidationModel);

			// Assert
			validationResult
				.IsValid
				.Should().BeTrue();

			datasetUploadValidationModel
				.ValidationResult
				.FieldValidationFailures
				.Count()
				.ShouldBeEquivalentTo(0);

			datasetUploadValidationModel
				.ValidationResult
				.HeaderValitionFailures
				.Count()
				.ShouldBeEquivalentTo(0);

			providerFieldValidator.Received(4).ValidateField(Arg.Any<Field>());
			headerValidator.Received(1).ValidateHeaders(Arg.Any<List<HeaderField>>());
		}

		[TestMethod]
		public void Validate_WhenSomeValidationFails_ShouldReflectInValidationResult()
		{
			// Arrange
			TableLoadResult tableLoadResult = new TableLoadResult
			{
				Rows = new List<RowLoadResult>
				{
					new RowLoadResult
					{
						Fields = new Dictionary<string, object>
						{
							{"Rid", "002Ba107013"},
							{"Parent Rid", null},
						},
						Identifier = null,
						IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
					},
					new RowLoadResult
					{
						Fields = new Dictionary<string, object>
						{
							{"Rid", "002Ba107016"},
							{"Parent Rid", null},
						}
					}
				}
			};

			DatasetDefinition datasetDefinition = CreateDatasetDefinition(4);
			ExcelPackage excelPackage = new ExcelPackage();
			List<ProviderSummary> providerSummaries = CreateProviderSummaries().Values.ToList();

			IExcelDatasetReader mockExcelDatasetReader = Substitute.For<IExcelDatasetReader>();
			mockExcelDatasetReader
				.Read(Arg.Any<ExcelPackage>(), Arg.Any<DatasetDefinition>())
				.Returns(tableLoadResult);

			IFieldValidator providerFieldValidator = Substitute.For<IFieldValidator>();
			providerFieldValidator.ValidateField(Arg.Any<Field>()).Returns(p => PassedValidationResult, p => PassedValidationResult, p => FailedValidationResult, p => FailedValidationResult);

			FieldDefinition anyFieldDefinition = new FieldDefinition();
			IHeaderValidator headerValidator = Substitute.For<IHeaderValidator>();
			headerValidator.ValidateHeaders(Arg.Any<IList<HeaderField>>()).Returns(new List<HeaderValidationResult>()
			{
				new HeaderValidationResult(anyFieldDefinition),
				new HeaderValidationResult(anyFieldDefinition)
			});


			DatasetUploadValidationModel datasetUploadValidationModel =
				new DatasetUploadValidationModel(excelPackage, () => providerSummaries, datasetDefinition, tableLoadResult);

			DatasetItemValidator validatorUnderTest = new DatasetItemValidator(mockExcelDatasetReader)
			{
				FieldValidators = new List<IFieldValidator>() { providerFieldValidator }
			};

			// Act
			ValidationResult validationResult = validatorUnderTest.Validate(datasetUploadValidationModel);

			// Assert
			providerFieldValidator.Received(4).ValidateField(Arg.Any<Field>());

			validationResult
				.IsValid
				.Should().BeTrue();

			datasetUploadValidationModel
				.ValidationResult
				.FieldValidationFailures
				.Count()
				.ShouldBeEquivalentTo(2);
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
					LACode = null,
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

		private DatasetDefinition CreateDatasetDefinition(int? numberOfTableDefinitions = null)
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
					Name = "Rid",
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
					Name = "Parent Rid",
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
					Name = "UPIN",
					Required = false,
					Type = FieldType.String
				},
				new FieldDefinition
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
				},
				new FieldDefinition
				{
					Description = "Describes the high level type of provider",
					Id = "1100005",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "Provider Type",
					Required = false,
					Type = FieldType.String
				},
				new FieldDefinition
				{
					Description = "Describes the sub type of the provider",
					Id = "1100006",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "Provider SubType",
					Required = false,
					Type = FieldType.String
				},
				new FieldDefinition
				{
					Description = "The date the provider opened or will open",
					Id = "1100007",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "Date Opened",
					Required = false,
					Type = FieldType.DateTime
				},
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
				},
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
				},
				new FieldDefinition
				{
					Description = "The local authority assosciated with the provider",
					Id = "1100010",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "Local Authority",
					Required = false,
					Type = FieldType.String
				},
				new FieldDefinition
				{
					Description = "Current year high needs students aged 16-19",
					Id = "1100011",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "High Needs Students 16-19",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Current year high needs students aged 19-24",
					Id = "1100012",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "High Needs Students 19-24",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Count of high needs students aged 16-19 from the ILR R04 collection",
					Id = "1100013",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R04 High Needs Students 16-19",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Count of high needs students aged 19-24 from the ILR R04 collection",
					Id = "1100014",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R04 High Needs Students 19-24",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Count of high needs students aged 16-19 from the ILR R14 collection",
					Id = "1100015",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R14 High Needs Students 16-19",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Count of high needs students aged 19-24 from the ILR R14 collection",
					Id = "1100016",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R14 High Needs Students 19-24",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Description to be provided as part of the schema",
					Id = "1100017",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "Sp SSF High Needs",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Description to be provided as part of the schema",
					Id = "1100017",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "SSF High Needs",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Count of high needs students aged 16-19 from the ILR R46 collection",
					Id = "1100018",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R46 High Needs Students 1619",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Count of high needs students aged 16-19 from the ILR R46 collection",
					Id = "1100019",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R46 High Needs Students 1924",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Description to be provided as part of the schema",
					Id = "1100020",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "R06 High Needs Students",
					Required = false,
					Type = FieldType.Decimal
				},
				new FieldDefinition
				{
					Description = "Description to be provided as part of the schema",
					Id = "1100021",
					IdentifierFieldType = null,
					MatchExpression = null,
					Maximum = null,
					Minimum = null,
					Name = "Place Value",
					Required = false,
					Type = FieldType.Decimal
				}
			};

			if (numberOfTableDefinitions.HasValue)
			{
				fieldDefinitions = fieldDefinitions.Take(numberOfTableDefinitions.Value).ToList();
			}

			var definitionsToExtract = new DatasetDefinition
			{
				Description = "High Needs Student Numbers",
				Id = "1000000",
				Name = "High Needs Student Numbers",
				TableDefinitions = new List<TableDefinition>()
				{
					new TableDefinition
					{
						Description = "High Needs",
						FieldDefinitions = fieldDefinitions,
						Id = "1100000",
						Name = "High Needs"
					}
				}
			};
			return definitionsToExtract;
		}

		//private IList<ProviderSummary> CreateAllProviderSummaries()
		//{
		//	return new List<ProviderSummary>
		//	{
		//		new ProviderSummary()
		//		{
		//			Name = "Barnsley College",
		//			Authority = "Barnsley",

		//		}
		//	}
		//}

		private TableLoadResult CreateTableLoadResult(int? numberOfRows = null)
		{
			List<RowLoadResult> rowLoadResults = new List<RowLoadResult>
			{
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "002Ba107013"},
						{"Parent Rid", null},
						{"UPIN", 107013},
						{"Provider Name", "Barnsley College"},
						{"Provider Type", "16-18 Provider"},
						{"Provider SubType", "General FE and Tertiary"},
						{"Date Opened", null},
						{"URN", 130524},
						{"Establishment Number", 8001},
						{"Local Authority", "Barnsley"},
						{"High Needs Students 16-19", 209},
						{"High Needs Students 19-24", 125},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", null}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				},
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "002Ba107016"},
						{"Parent Rid", null},
						{"UPIN", 107016},
						{"Provider Name", "Independent Training Services Limited"},
						{"Provider Type", "16-18 Provider"},
						{"Provider SubType", "Independent Private Provider"},
						{"Date Opened", null},
						{"URN", 0},
						{"Establishment Number", 0},
						{"Local Authority", "Barnsley"},
						{"High Needs Students 16-19", 15},
						{"High Needs Students 19-24", 0},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", null}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				},
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "00387986c41"},
						{"Parent Rid", null},
						{"UPIN", 139305},
						{"Provider Name", "Godalming College"},
						{"Provider Type", "Academy"},
						{"Provider SubType", "Academies"},
						{"Date Opened", null},
						{"URN", 145004},
						{"Establishment Number", 8600},
						{"Local Authority", "Surrey"},
						{"High Needs Students 16-19", 5},
						{"High Needs Students 19-24", 0},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", null}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				},
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "003Ba105154"},
						{"Parent Rid", null},
						{"UPIN", 105154},
						{"Provider Name", "Bath College"},
						{"Provider Type", "16-18 Provider"},
						{"Provider SubType", "General FE and Tertiary"},
						{"Date Opened", null},
						{"URN", 130558},
						{"Establishment Number", 8009},
						{"Local Authority", "Bath and North East Somerset"},
						{"High Needs Students 16-19", 54},
						{"High Needs Students 19-24", 16},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", null}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				},
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "003Ba114494"},
						{"Parent Rid", null},
						{"UPIN", 119570},
						{"Provider Name", "Beechen Cliff School"},
						{"Provider Type", "Academy"},
						{"Provider SubType", "Academies"},
						{"Date Opened", null},
						{"URN", 136520},
						{"Establishment Number", 5400},
						{"Local Authority", "Bath and North East Somerset"},
						{"High Needs Students 16-19", 2},
						{"High Needs Students 19-24", 0},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", null}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				},
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "003Ba114496"},
						{"Parent Rid", null},
						{"UPIN", 114496},
						{"Provider Name", "Chew Valley School"},
						{"Provider Type", "School Sixth Form"},
						{"Provider SubType", "School Sixth Form"},
						{"Date Opened", null},
						{"URN", 109306},
						{"Establishment Number", 4130},
						{"Local Authority", "Bath and North East Somerset"},
						{"High Needs Students 16-19", null},
						{"High Needs Students 19-24", null},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", 4}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				},
				new RowLoadResult
				{
					Fields = new Dictionary<string, object>
					{
						{"Rid", "003Ba114501"},
						{"Parent Rid", null},
						{"UPIN", 119966},
						{"Provider Name", "Hayesfield Girls School"},
						{"Provider Type", "Academy"},
						{"Provider SubType", "Academies"},
						{"Date Opened", null},
						{"URN", 136966},
						{"Establishment Number", 4107},
						{"Local Authority", "Bath and North East Somerset"},
						{"High Needs Students 16-19", 3},
						{"High Needs Students 19-24", 0},
						{"R04 High Needs Students 16-19", null},
						{"R04 High Needs Students 19-24", null},
						{"R14 High Needs Students 16-19", null},
						{"R14 High Needs Students 19-24", null},
						{"Sp SSF High Needs", null},
						{"SSF High Needs", null},
						{"R46 High Needs Students 1619", null},
						{"R46 High Needs Students 1924", null},
						{"R06 High Needs Students", null},
						{"Place Value", null}
					},
					Identifier = null,
					IdentifierFieldType = IdentifierFieldType.EstablishmentNumber
				}
			};

			if (numberOfRows.HasValue)
			{
				rowLoadResults = rowLoadResults.Take(numberOfRows.Value).ToList();
			}

			var tableLoadResult = new TableLoadResult
			{
				GlobalErrors = new List<DatasetValidationError>(),
				Rows = rowLoadResults
			};
			return tableLoadResult;
		}
	}
}