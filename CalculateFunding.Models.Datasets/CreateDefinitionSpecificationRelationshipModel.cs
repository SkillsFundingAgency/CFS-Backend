namespace CalculateFunding.Models.Datasets
{
    public class CreateDefinitionSpecificationRelationshipModel
    {
        public string DatasetDefinitionId { get; set; }

        public string SpecificationId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsSetAsProviderData { get; set; }

        public bool UsedInDataAggregations { get; set; }

        public bool ConverterEnabled { get; set; }
    }
}
