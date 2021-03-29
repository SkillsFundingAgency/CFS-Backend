using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class ExtraHeaderFieldValidator : IFieldValidator
    {
        private readonly IList<FieldDefinition> _fieldDefinitions;

        public ExtraHeaderFieldValidator(IList<FieldDefinition> fieldDefinitions)
        {
            _fieldDefinitions = fieldDefinitions;
        }

        public FieldValidationResult ValidateField(Field field)
        {
            if(_fieldDefinitions.Where(x => MatchColumn(x, field.Value.ToString())).Count() == 0)
            {
                return new FieldValidationResult(field, FieldValidationResult.ReasonForFailure.ExtraHeaderField);
            }

            return null;
        }

        private static bool MatchColumn(FieldDefinition x, string valAsString)
        {
            return !string.IsNullOrEmpty(x.MatchExpression) ? Regex.IsMatch(valAsString, WildCardToRegular(x.MatchExpression)) : string.Equals(valAsString.Trim(), x.Name.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }
    }
}
