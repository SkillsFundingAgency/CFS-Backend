using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.IntegrationTests.Common.Data
{
    public abstract class NoPartitionKeyCosmosBulkDataContext : CosmosBulkDataContext
    {
        protected NoPartitionKeyCosmosBulkDataContext(IConfiguration configuration,
            string cosmosCollectionName,
            string templateResourceName,
            Assembly resourceAssembly)
            : base(configuration,
                cosmosCollectionName,
                templateResourceName,
                resourceAssembly)
        {
        }

        protected override string GetPartitionKey(JsonElement content) => null;
    }
}