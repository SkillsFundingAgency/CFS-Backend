using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public abstract class PolicyCollectionDataContext : CosmosBulkDataContext
    {
        public PolicyCollectionDataContext(IConfiguration configuration, string templateResourceName)
            : base(configuration,
                "policy",
                templateResourceName,
                typeof(PolicyCollectionDataContext).Assembly)
        {
        }
        
        protected override string GetPartitionKey(JsonElement content) => GetElement(content, "id")
            .GetString();
    }
}
