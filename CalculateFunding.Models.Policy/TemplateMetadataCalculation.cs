using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using System.Collections.Generic;

namespace CalculateFunding.Models.Policy
{
    public class TemplateMetadataCalculation
    {
        public string Name { get; set; }
        public uint TemplateCalculationId { get; set; }
        public CalculationValueFormat ValueFormat { get; set; }
        public IEnumerable<string> AllowedEnumTypeValues { get; set; }
        public CalculationType Type { get; set; }
        public string FormulaText { get; set; }
        public AggregationType AggregationType { get; set; }
        public GroupRate GroupRate { get; set; }
        public PercentageChangeBetweenAandB PercentageChangeBetweenAandB { get; set; }
    }
}
