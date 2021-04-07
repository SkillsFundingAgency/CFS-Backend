using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter.ExcelFormatter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentValidation;
using Microsoft.Azure.Storage.Blob;
using OfficeOpenXml;
using Serilog;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class DatasetUploadValidationModelValidator : AbstractValidator<DatasetUploadValidationModel>
    {
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IExcelDatasetReader _excelDatasetReader;

        public IHeaderValidator HeaderValidator { private get; set; }
        public IList<IFieldValidator> FieldValidators { private get; set; }
        public IBulkFieldValidator BulkValidator { private get; set; }
        public IList<IExcelErrorFormatter> ExcelFieldFormatters { get; set; }

        private bool _isValid = true;

        public DatasetUploadValidationModelValidator(
            IExcelDatasetReader excelDatasetReader,
            IBlobClient blobClient,
            ILogger logger)
        {
            _logger = logger;
            _blobClient = blobClient;
            _excelDatasetReader = excelDatasetReader;

            RuleFor(model => model)
                .NotNull()
                .CustomAsync(async (validationModel, context, cancellationToken) =>
                {
                    ExcelPackage excelPackage = validationModel.ExcelPackage;

					validationModel.Data = excelDatasetReader.Read(excelPackage, validationModel.DatasetDefinition, false, true);

                    IEnumerable<ProviderSummary> providerSummaries = validationModel.ProviderSummaries();

                    await Validate(excelPackage, validationModel, providerSummaries);

                    if (!_isValid)
                    {
                        // message will not be surfaced, purely to return an invalid result.
                        context.AddFailure("Excel did not validate");
                    }
                });
        }

        private async Task Validate(
            DatasetUploadValidationModel validationModel,
            IEnumerable<ProviderSummary> providerSummaries)
        {
            IList<FieldDefinition> fieldDefinitions = validationModel.DatasetDefinition.TableDefinitions.First().FieldDefinitions;
            IHeaderValidator headerValidator = GetHeaderValidator(fieldDefinitions);
            IList<HeaderValidationResult> headerValidationFailures = new List<HeaderValidationResult>();
            ConcurrentBag<FieldValidationResult> fieldValidationFailures = new ConcurrentBag<FieldValidationResult>();

            IList<HeaderValidationResult> headerValidationResults
                = headerValidator.ValidateHeaders(validationModel.Data.RetrievedHeaderFields.Keys.ToList());

            bool success;
            string errorMessage;
            List<TableLoadResult> latestTableLoadResults;

            (success, errorMessage, latestTableLoadResults) = await ReadExcelDatasetData(validationModel.DatasetDefinition, validationModel.LatestBlobFileName);
            if (!success)
            {
                _logger.Error(errorMessage);
                throw new NonRetriableException("Failed Validation - Error while reading dataset file");
            }

            TableLoadResult tableLoadResultToMerge = validationModel.Data.TableLoadResult;
            TableLoadResult latestTableLoadResult =
                latestTableLoadResults.FirstOrDefault(_ => _.TableDefinition?.Name == tableLoadResultToMerge.TableDefinition.Name);
            RowLoadResult[] newRows = tableLoadResultToMerge.GetRowsMissingFrom(latestTableLoadResult).ToArray();

            if (newRows.Length > 0)
            {
                if (headerValidationResults.Count > 0)
                {
                    headerValidationFailures.AddRange(headerValidationResults);

                    IFieldValidator providerIdentifierMissingAllDataSchemaFieldsValidator
                        = GetProviderIdentifierMissingAllDataSchemaFieldsValidator();

                    for (int index = 0; index < newRows.Count(); index++)
                    {
                        RowLoadResult newRow = newRows[index];

                        // +1 for header field
                        // +1 for ExcelPackage row index starts from 1, instead of 0
                        int rowIndex = tableLoadResultToMerge.Rows.IndexOf(newRow) + 2; 
                        
                        foreach (KeyValuePair<string, object> fieldKeyValue in newRow.Fields)
                        {
                            Field field = GetFieldFromKeyValuePair(fieldDefinitions, rowIndex, fieldKeyValue, validationModel.Data.RetrievedHeaderFields);
                            FieldValidationResult fieldValidationResult = providerIdentifierMissingAllDataSchemaFieldsValidator.ValidateField(field);

                            if (fieldValidationResult != null)
                            {
                                fieldValidationFailures.Add(fieldValidationResult);
                            }
                        }
                    }
                }
            }

            if (fieldValidationFailures.Count > 0)
            {
                validationModel.ValidationResult = new DatasetUploadValidationResult
                {
                    FieldValidationFailures = fieldValidationFailures,
                    HeaderValitionFailures = headerValidationFailures
                };
                return;
            }

            int headerRowIndex = 1;
            IFieldValidator extraHeaderFieldValidator = GetExtraHeaderFieldValidator(fieldDefinitions);
            List<FieldDefinition> headerFieldDefinitions = tableLoadResultToMerge.TableDefinition.FieldDefinitions;

            foreach (KeyValuePair<string, int> fieldNameIndex in validationModel.Data.RetrievedHeaderFields)
            {
                FieldValidationResult extraHeaderFieldValidationResult =
                extraHeaderFieldValidator.ValidateField(
                    new Field(
                        new DatasetUploadCellReference(headerRowIndex, fieldNameIndex.Value),
                        fieldNameIndex.Key,
                        null));

                if (extraHeaderFieldValidationResult != null)
                {
                    fieldValidationFailures.Add(extraHeaderFieldValidationResult);
                }
            }

            if (fieldValidationFailures.Count > 0)
            {
                validationModel.ValidationResult = new DatasetUploadValidationResult
                {
                    FieldValidationFailures = fieldValidationFailures,
                    HeaderValitionFailures = headerValidationFailures
                };
                return;
            }

            if (validationModel.DatasetEmptyFieldEvaluationOption == DatasetEmptyFieldEvaluationOption.AsNull)
            {
                IEnumerable<HeaderValidationResult> requiredNullValueHeaderValidationFailures = 
                    headerValidationResults.Where(_ => _.FieldDefinitionValidated.Required);

                if (requiredNullValueHeaderValidationFailures.Count() > 0)
                {
                    validationModel.ValidationResult = new DatasetUploadValidationResult
                    {
                        FieldValidationFailures = fieldValidationFailures,
                        HeaderValitionFailures = requiredNullValueHeaderValidationFailures
                    };
                    return;
                }
            }

            IList<IFieldValidator> fieldValidators = GetFieldValidators(providerSummaries, validationModel.DatasetDefinition.ValidateProviders);
            IBulkFieldValidator bulkFieldValidator = GetBulkFieldValidator();

            List<RowLoadResult> allRows = validationModel.Data.TableLoadResult.Rows;
            IList<Field> allFieldsToBeValidated = RetrieveAllFields(allRows, fieldDefinitions, validationModel.Data.RetrievedHeaderFields);

            Parallel.ForEach(allFieldsToBeValidated, (Field field, ParallelLoopState state) =>
            {
                FieldValidationResult validationResult = ReturnFirstOnFailureOrDefault(fieldValidators, field);
                if (validationResult != null)
                {
                    fieldValidationFailures.Add(validationResult);
                }
            });

            IList<FieldValidationResult> bulkFieldValidationResults =
                bulkFieldValidator.ValidateAllFields(allFieldsToBeValidated);

            foreach (FieldValidationResult validationResult in bulkFieldValidationResults)
            {
                fieldValidationFailures.Add(validationResult);
            }

            validationModel.ValidationResult = new DatasetUploadValidationResult
            {
                FieldValidationFailures = fieldValidationFailures,
                HeaderValitionFailures = headerValidationFailures
            };
        }

        private async Task Validate(
            ExcelPackage excelPackage, 
            DatasetUploadValidationModel validationModel,
            IEnumerable<ProviderSummary> providerSummaries)
        {
            await Validate(validationModel, providerSummaries);

            _isValid = validationModel.ValidationResult.IsValid();

            IList<IExcelErrorFormatter> excelFormatters = GetAllExcelFormatters(excelPackage);
            foreach (IExcelErrorFormatter excelErrorFormatter in excelFormatters)
            {
                excelErrorFormatter.FormatExcelSheetBasedOnErrors(validationModel.ValidationResult);
            }
        }

        private async Task<(bool success, string errorMessage, List<TableLoadResult> tableLoadResults)> ReadExcelDatasetData(
            DatasetDefinition datasetDefinition,
            string blobFileName)
        {
            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(blobFileName);

            if (blob == null)
            {
                return (false, $"Failed to find blob with path: {blobFileName}", new List<TableLoadResult>());
            }

            await using Stream datasetStream = await _blobClient.DownloadToStreamAsync(blob);

            if (datasetStream == null || datasetStream.Length == 0)
            {
                return (false, $"Blob {blob.Name} contains no data", new List<TableLoadResult>());
            }

            List<TableLoadResult> tableLoadResults = _excelDatasetReader.Read(datasetStream, datasetDefinition).ToList();
            return (true, null, tableLoadResults);
        }

        private IList<Field> RetrieveAllFields(List<RowLoadResult> allRows, IList<FieldDefinition> fieldDefinitions, IDictionary<string, int> headersAndColumns)
        {
            IList<Field> allFieldsToBeValidated = new List<Field>();

            for (int rowIndex = 2, index = 0; index < allRows.Count; rowIndex++, index++)
            {
                RowLoadResult row = allRows[index];

                foreach (KeyValuePair<string, object> fieldKeyValue in row.Fields)
                {
                    Field field = GetFieldFromKeyValuePair(fieldDefinitions, rowIndex, fieldKeyValue, headersAndColumns);
                    allFieldsToBeValidated.Add(field);
                    
                }
            }

            return allFieldsToBeValidated;
        }

        private Field GetFieldFromKeyValuePair(IList<FieldDefinition> fieldDefinitions, int row, KeyValuePair<string, object> keyValue, IDictionary<string, int> headersAndColumns)
        {
            return new Field(new DatasetUploadCellReference(row, headersAndColumns[keyValue.Key]), keyValue.Value,
                fieldDefinitions.FirstOrDefault(f => f.Name == keyValue.Key));
        }

		private IList<IFieldValidator> GetFieldValidators(
            IEnumerable<ProviderSummary> providerSummaries,
            bool validateProviders)
		{
			if (FieldValidators.IsNullOrEmpty())
			{
				IFieldValidator requiredValidator = new RequiredValidator();
				IFieldValidator providerBlankValidator = new ProviderIdentifierBlankValidator();
				IFieldValidator dataTypeMismatchFieldValidator = new DatatypeMismatchFieldValidator();
				IFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

                List<IFieldValidator> fieldValidators = new List<IFieldValidator>
                {
                    providerBlankValidator,
                    requiredValidator,
                    dataTypeMismatchFieldValidator,
                    maxAndMinFieldValidator,
                };

                if (validateProviders)
                {
                    IFieldValidator providerExistsValidator = new ProviderExistsValidator(providerSummaries.ToList());
                    fieldValidators.Add(providerExistsValidator);
                }
                else
                {
                    IFieldValidator providerIdRangeValidator = new ProviderIdRangeValidator();
                    fieldValidators.Add(providerIdRangeValidator);
                }

                return fieldValidators;
            }

            return FieldValidators;
        }

        private IHeaderValidator GetHeaderValidator(IList<FieldDefinition> fieldDefinitions)
        {
            return HeaderValidator ?? new RequiredHeaderExistsValidator(fieldDefinitions);
        }

        private IFieldValidator GetProviderIdentifierMissingAllDataSchemaFieldsValidator()
        {
            return new ProviderIdentifierMissingAllDataSchemaFieldsValidator();
        }

        private IFieldValidator GetExtraHeaderFieldValidator(IList<FieldDefinition> fieldDefinitions)
        {
            return new ExtraHeaderFieldValidator(fieldDefinitions);
        }

        private IBulkFieldValidator GetBulkFieldValidator()
        {
            return BulkValidator ?? new ProviderDuplicatesExistsValidator();
        }

        private IList<IExcelErrorFormatter> GetAllExcelFormatters(ExcelPackage excelPackage)
        {
            return new List<IExcelErrorFormatter>
            {
                new ExcelFieldErrorFormatter(excelPackage),
                new ExcelHeaderErrorFormatter(excelPackage)
            };
        }

        private FieldValidationResult ReturnFirstOnFailureOrDefault(IList<IFieldValidator> fieldValidators, Field field)
        {
            foreach (IFieldValidator validator in fieldValidators)
            {
                FieldValidationResult validationResult = validator.ValidateField(field);
                if (validationResult != null)
                {
                    return validationResult;
                }
            }

            return null;
        }

        
    }
}