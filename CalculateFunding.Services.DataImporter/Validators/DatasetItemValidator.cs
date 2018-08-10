using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.DataImporter.Validators.Models;
using FluentValidation;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.DataImporter.ExcelFormatter;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class DatasetItemValidator:  AbstractValidator<DatasetUploadValidationModel>
    {
        private readonly IExcelDatasetReader _excelDatasetReader;

		public IList<IFieldValidator> FieldValidators { get; set; }

		public IHeaderValidator HeaderValidator { get; set; }

		public IExcelFieldFormatter ExcelFieldFormatter { get; set; }

        public DatasetItemValidator(IExcelDatasetReader excelDatasetReader)
        {
            _excelDatasetReader = excelDatasetReader;

            RuleFor(model => model)
             .NotNull()
             .Custom((validationModel, context) => {

                 ExcelPackage excelPackage = validationModel.ExcelPackage;

                 validationModel.Data = _excelDatasetReader.Read(excelPackage, validationModel.DatasetDefinition);

                 IEnumerable<ProviderSummary> providerSummaries = validationModel.ProviderSummaries();

		         Validate(excelPackage, validationModel, providerSummaries);
	            });

			
        }

        private void Validate(ExcelPackage excelPackage, DatasetUploadValidationModel validationModel, IEnumerable<ProviderSummary> providerSummaries)
        {
	        IList<FieldDefinition> fieldDefinitions = validationModel.DatasetDefinition.TableDefinitions.First().FieldDefinitions;

			IList<IFieldValidator> fieldValidators = GetFieldValidators(providerSummaries);
	        IHeaderValidator headerValidator = GetHeaderValidator(fieldDefinitions);

	        IList<FieldValidationResult> fieldValidationFailures = new List<FieldValidationResult>();
	        IList<HeaderValidationResult> headerValidationFailures = new List<HeaderValidationResult>();
	        

            IEnumerable<RowLoadResult> dataRows = validationModel.Data.Rows;

	        for (var rowIndex = 0; rowIndex < validationModel.Data.Rows.Count; rowIndex++)
	        {
		        int columnIndex = 0;
				RowLoadResult row = validationModel.Data.Rows[rowIndex];
		        if (rowIndex == 0)
		        {
			        IList<HeaderValidationResult> headerValidationResults =
				        headerValidator.ValidateHeaders(row.Fields.Select(f => new HeaderField(f.Key)).ToList());
					headerValidationFailures.AddRange(headerValidationResults);
		        }

		        foreach (KeyValuePair<string, object> field in row.Fields)
		        {
			        FieldDefinition fieldDefinition = fieldDefinitions.FirstOrDefault(m => m.Name == field.Key);
			        Field fieldModel = new Field(new DatasetUploadCellReference(rowIndex+1, columnIndex), field.Value,
				        fieldDefinition);
			        FieldValidationResult validationResult = ReturnFirstOnFailureOrDefault(fieldValidators, fieldModel);
			        if (validationResult != null)
			        {
				        fieldValidationFailures.Add(validationResult);
			        }

			        columnIndex++;
		        }
	        }

	        validationModel.ValidationResult = new DatasetUploadValidationResult()
	        {
				FieldValidationFailures = fieldValidationFailures,
				HeaderValitionFailures = headerValidationFailures
	        };
        }

		private IList<IFieldValidator> GetFieldValidators(IEnumerable<ProviderSummary> providerSummaries)
		{
			if (FieldValidators.IsNullOrEmpty())
			{
				IFieldValidator providerExistingValidator = new ProviderExistsValidator(providerSummaries.ToList());
				return new List<IFieldValidator>() {providerExistingValidator};
			}

			return FieldValidators;
		}

	    private IHeaderValidator GetHeaderValidator(IList<FieldDefinition> fieldDefinitions)
	    {
			return HeaderValidator ?? new RequiredHeaderExistsValidator(fieldDefinitions);
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
