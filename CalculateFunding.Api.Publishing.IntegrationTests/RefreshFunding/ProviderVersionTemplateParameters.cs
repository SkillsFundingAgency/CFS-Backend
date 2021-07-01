using CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class ProviderVersionTemplateParameters
    {
        public string Id { get; set; }

        public Provider[] Providers { get; set; }
    }
}