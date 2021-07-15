namespace CalculateFunding.Models.Graph
{
    public class CalculationDataFieldRelationship
    {
        public const string ToIdField = "IsReferencedInCalculation";
        public const string FromIdField = "ReferencesDataField";

        public Calculation Calculation { get; set; }

        public DataField DataField { get; set; }
    }
}
