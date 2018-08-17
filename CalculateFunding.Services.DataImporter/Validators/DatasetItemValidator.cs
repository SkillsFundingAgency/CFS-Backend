using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.DataImporter.ExcelFormatter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentValidation;
using OfficeOpenXml;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class DatasetItemValidator : AbstractValidator<DatasetUploadValidationModel>
    {
        private readonly IExcelDatasetReader _excelDatasetReader;
        public IHeaderValidator HeaderValidator { private get; set; }
        public IList<IFieldValidator> FieldValidators { private get; set; }
        public IBulkFieldValidator BulkValidator { private get; set; }
        public IList<IExcelErrorFormatter> ExcelFieldFormatter { get; set; }

        private bool _isValid = true;

        public DatasetItemValidator(IExcelDatasetReader excelDatasetReader)
        {
            _excelDatasetReader = excelDatasetReader;

            RuleFor(model => model)
                .NotNull()
                .Custom((validationModel, context) =>
                {
                    ExcelPackage excelPackage = validationModel.ExcelPackage;

					validationModel.Data = _excelDatasetReader.Read(excelPackage, validationModel.DatasetDefinition, false);

                    IEnumerable<ProviderSummary> providerSummaries = validationModel.ProviderSummaries();

                    Validate(excelPackage, validationModel, providerSummaries);

                    if (!_isValid)
                    {
                        // message will not be surfaced, purely to return an invalid result.
                        context.AddFailure("Excel did not validate");
                    }
                });
        }

        private void Validate(ExcelPackage excelPackage, DatasetUploadValidationModel validationModel,
            IEnumerable<ProviderSummary> providerSummaries)
        {
            IList<FieldDefinition> fieldDefinitions =
                validationModel.DatasetDefinition.TableDefinitions.First().FieldDefinitions;

            IHeaderValidator headerValidator = GetHeaderValidator(fieldDefinitions);
            IList<IFieldValidator> fieldValidators = GetFieldValidators(providerSummaries);
            IBulkFieldValidator bulkFieldValidator = GetBulkFieldValidator();

            IList<IExcelErrorFormatter> excelFormatters = GetAllExcelFormatters(excelPackage);

            IList<HeaderValidationResult> headerValidationFailures = new List<HeaderValidationResult>();

            List<RowLoadResult> allRows = validationModel.Data.TableLoadResult.Rows;
            IList<Field> allFieldsToBeValidated = RetrieveAllFields(allRows, fieldDefinitions, validationModel.Data.RetrievedHeaderFields);

            headerValidationFailures.AddRange(headerValidator.ValidateHeaders(validationModel.Data.RetrievedHeaderFields.Keys.ToList()));

            ConcurrentBag<FieldValidationResult> fieldValidationFailures = new ConcurrentBag<FieldValidationResult>();

            Parallel.ForEach(allFieldsToBeValidated, (Field field, ParallelLoopState state) =>
            {
                FieldValidationResult validationResult = ReturnFirstOnFailureOrDefault(fieldValidators, field);
                if (validationResult != null)
                {
                    fieldValidationFailures.Add(validationResult);
                }
            });


            IList<FieldValidationResult> bulkFieldValidationResults =
                bulkFieldValidator
                    .ValidateAllFields(allFieldsToBeValidated);

            foreach (FieldValidationResult validationResult in bulkFieldValidationResults)
            {
                fieldValidationFailures.Add(validationResult);
            }

            validationModel.ValidationResult = new DatasetUploadValidationResult
            {
                FieldValidationFailures = fieldValidationFailures,
                HeaderValitionFailures = headerValidationFailures
            };

            _isValid = validationModel.ValidationResult.IsValid();

            foreach (var excelErrorFormatter in excelFormatters)
            {
                excelErrorFormatter.FormatExcelSheetBasedOnErrors(validationModel.ValidationResult);
            }

            excelPackage.Save();
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

		private IList<IFieldValidator> GetFieldValidators(IEnumerable<ProviderSummary> providerSummaries)
		{
			if (FieldValidators.IsNullOrEmpty())
			{
				IFieldValidator requiredValidator = new RequiredValidator();
				IFieldValidator providerBlankValidator = new ProviderIdentifierBlankValidator();
				IFieldValidator dataTypeMismatchFieldValidator = new DatatypeMismatchFieldValidator();
				IFieldValidator providerExistsValidator = new ProviderExistsValidator(providerSummaries.ToList());
				IFieldValidator maxAndMinFieldValidator = new MaxAndMinFieldValidator();

				return new List<IFieldValidator>
				{
					providerBlankValidator,
					requiredValidator,
					dataTypeMismatchFieldValidator,
					providerExistsValidator,
                    maxAndMinFieldValidator,
                    providerExistsValidator
                };
            }

            return FieldValidators;
        }

        private IHeaderValidator GetHeaderValidator(IList<FieldDefinition> fieldDefinitions)
        {
            return HeaderValidator ?? new RequiredHeaderExistsValidator(fieldDefinitions);
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