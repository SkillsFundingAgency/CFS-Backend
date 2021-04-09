using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class ProviderDuplicatesExistsValidator : IBulkFieldValidator
    {
	    public IList<FieldValidationResult> ValidateAllFields(IList<Field> allFields)
	    {
		    var filteredFields = allFields.Where(PreValidateField);

		    var fieldGrouping = 
			    filteredFields
				    .GroupBy(f => new {f.FieldDefinition.Name, f.Value})
				    .Where(g => g.ToList().Count > 1);

		    List<FieldValidationResult> fieldValidationResults = 
			    fieldGrouping
				    .SelectMany(g => g.ToList())
				    .Select(f => new FieldValidationResult(f, DatasetCellReasonForFailure.DuplicateEntriesInTheProviderIdColumn))
				    .ToList();

		    return fieldValidationResults;
	    }

	    private static bool PreValidateField(Field field)
	    {
		    return field.FieldDefinition.IdentifierFieldType.HasValue && !string.IsNullOrEmpty(field.Value?.ToString());
	    }
	}
}
