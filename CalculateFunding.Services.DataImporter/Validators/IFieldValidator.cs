using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.DataImporter.Validators.Models;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public interface IFieldValidator
    {
	    FieldValidationResult ValidateField(Field field);
    }
}
