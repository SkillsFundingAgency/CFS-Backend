using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class RequiredValidator : IFieldValidator
    {
	    public FieldValidationResult ValidateField(Field field)
	    {
		    if (field.FieldDefinition.Required && string.IsNullOrEmpty(field.Value?.ToString()))
		    {
				return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.DataTypeMismatch);
		    }
		    return null;
	    }
    }
}
