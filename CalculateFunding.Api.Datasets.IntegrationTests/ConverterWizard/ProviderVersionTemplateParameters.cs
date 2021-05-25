using CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class ProviderVersionTemplateParameters
    {
        public string Id { get; set; }

        public Provider[] Providers { get; set; }
    }
}