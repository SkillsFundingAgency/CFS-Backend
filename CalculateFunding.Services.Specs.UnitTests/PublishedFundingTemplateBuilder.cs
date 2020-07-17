using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class PublishedFundingTemplateBuilder : TestEntityBuilder
    {
        private string _templateVersion;

        public PublishedFundingTemplateBuilder WithTemplateVersion(string templateVersion)
        {
            _templateVersion = templateVersion;

            return this;
        }

        public PublishedFundingTemplate Build()
        {
            return new PublishedFundingTemplate
            {
                TemplateVersion = _templateVersion
            };
        }
    }
}
