using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CalculateFunding.Api.Publishing.IntegrationTests.Data
{
    public class ProviderSourceDatasetContext 
        : CosmosBulkDataContext
    {
        public ProviderSourceDatasetContext(IConfiguration configuration)
            : base(configuration,
            "providerdatasets",
            "CalculateFunding.Api.Publishing.IntegrationTests.Resources.ProviderSourceDatasetTemplate",
            typeof(ProviderSourceDatasetContext).Assembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                PROVIDERID = documentData.ProviderId,
                SPECIFICATIONID = documentData.SpecificationId,
                DATARELATIONSHIPID = documentData.DataRelationshipId,
                NOW = now
            };

        protected override string GetPartitionKey(JsonElement content)
            => GetElement(content, "providerId").GetString();
    }
}
