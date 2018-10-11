namespace CalculateFunding.Models.Datasets
{
    public class AggregatedField
    {
        public AggregatedFieldType FieldType { get; set; }

        public decimal? Value { get; set; }

        public string FieldDefinitionName { get; set; }

        public string FieldDefinitionId { get; set; }
    }
}
