using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.DataImporter.Validators.Models;
using static CalculateFunding.Services.DataImporter.Validators.Models.HeaderValidationResult;

namespace CalculateFunding.Services.DataImporter.Validators
{
    public class DuplicateHeaderValidator : IHeaderValidator
    {
        private readonly IDictionary<string, FieldDefinition> _fieldDefinitions;

        public DuplicateHeaderValidator(IEnumerable<FieldDefinition> fieldDefinitions)
        {
            _fieldDefinitions = fieldDefinitions.ToDictionary(_ => _.Name.ToLowerInvariant());
        }
		
        public IEnumerable<HeaderValidationResult> ValidateHeaders(IEnumerable<string> headerFields)
        {
            return headerFields
                .GroupBy(_ => _.ToLowerInvariant())
                .Where(_ => _.Count() > 1 &&
                            _fieldDefinitions.ContainsKey(_.Key))
                .Select(_ => CreateResultRequiringBackgroundColourKey(_fieldDefinitions[_.Key], 
                    DatasetCellReasonForFailure.DuplicateColumnHeader))
                .ToArray();
        }
    }
}