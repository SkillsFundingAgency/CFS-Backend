namespace CalculateFunding.Models.Datasets.ViewModels
{
    public class DatasetSpecificationRelationshipViewModel : Reference
    {
        public DatasetDefinitionViewModel Definition { get; set; }

        public string DatasetName { get; set; }

        public int? Version { get; set; }

        public string DatasetId { get; set; }
    }
}
