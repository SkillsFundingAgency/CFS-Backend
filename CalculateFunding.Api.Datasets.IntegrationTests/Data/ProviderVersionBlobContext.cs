using System.Reflection;
using System.Text.Json;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class ProviderVersionBlobContext : BlobBulkDataContext
    {
        public ProviderVersionBlobContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "providerversions",
            "CalculateFunding.Api.Datasets.IntegrationTests.Resources.ProviderVersionBlobTemplate",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                PROVIDERS = ((Provider[]) documentData.Providers).AsJson().Prettify()
            };

        protected override string GetBlobName(JsonDocument document) 
            => $"{GetId(document.RootElement)}.json";
    }
}