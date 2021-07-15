namespace CalculateFunding.Models.Graph
{
    public class DatasetRelationshipDataFieldRelationship
    {
        public const string ToIdField = "HasDatasetField";
        public const string FromIdField = "IsInDataset";

        public DatasetRelationship DatasetRelationship { get; set; }
        public DataField DataField { get; set; }
    }
}
