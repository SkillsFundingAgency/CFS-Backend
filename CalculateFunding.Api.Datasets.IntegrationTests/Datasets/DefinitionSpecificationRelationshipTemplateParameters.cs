using CalculateFunding.Common.ApiClient.DataSets.Models;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class DefinitionSpecificationRelationshipTemplateParameters
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string DefinitionId { get; set; }

        public string DefinitionName { get; set; }

        public string DatasetId { get; set; }

        public string DatasetName { get; set; }

        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public string Description { get; set; }

        public bool ConverterEnabled { get; set; }

        public DatasetRelationshipType DatasetRelationshipType { get; set; }
    }
}