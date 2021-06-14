using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class DatasetDefinitionTemplateParameters
    {
        public string Id { get; set; }

        public string Description { get; set; }
        
        public string Name { get; set; }

        public string FundingStreamId { get; set; }
        
        public int Version { get; set; }

        public bool ConverterEnabled { get; set; }

        public bool ConverterEligible { get; set; }

        public TableDefinition[] TableDefinitions { get; set; }
    }
}