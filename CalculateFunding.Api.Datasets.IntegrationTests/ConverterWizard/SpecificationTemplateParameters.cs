using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class SpecificationTemplateParameters
    {
        public string Id { get; set; }

        public string SpecificationVersionId { get; set; }

        public string Name { get; set; }

        public string FundingPeriodId { get; set; }

        public string FundingPeriodName { get; set; }

        public string ProviderVersionId { get; set; }

        public Reference[] FundingStreams { get; set; }

        public string Description { get; set; }

        public string[] DataDefinitionRelationshipIds { get; set; }

        public IDictionary<string, string> TemplateIds { get; set; }

        public ProviderSource ProviderSource { get; set; }

        public int? ProviderSnapshotId { get; set; }

        public int Version { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        public PublishStatus PublishStatus { get; set; }
    }
}