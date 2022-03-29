using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.Transforms
{
    [Binding]
    public class CalculationsTransforms : TransformsBase
    {
        [StepArgumentTransformation]
        public IEnumerable<CalculationResult> ToCalculationResults(Table calculationResultsTable)
        {
            EnsureTableHasData(calculationResultsTable);

            return calculationResultsTable.CreateSet<IntermediateCalculationResultType>()
                .Select(_ => _.ToCalculationResult())
                .ToArray();
        }

        private class IntermediateCalculationResultType
        {
            public string Id { get; set; }

            public string Value { get; set; }

            public string ValueType { get; set; }

            private string GetValueType() => ValueType ?? "decimal";

            public CalculationResult ToCalculationResult() =>
                new CalculationResult
                {
                    Id = Id,
                    Value = GetValueType() switch
                    {
                        "decimal" => decimal.TryParse(Value, System.Globalization.NumberStyles.Float, null, out decimal value) ? (object) value : null,
                        "boolean" => bool.TryParse(Value, out bool value) ? (object) value : null,
                        _ => Value
                    }
                };
        }
    }
}