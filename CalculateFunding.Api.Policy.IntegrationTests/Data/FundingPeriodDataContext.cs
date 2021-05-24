using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingPeriodDataContext : PolicyCollectionDataContext
    {
        public FundingPeriodDataContext(IConfiguration configuration) :
            base(configuration, "CalculateFunding.Api.Policy.IntegrationTests.Resources.FundingPeriodTemplate")
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                NAME = documentData.Name,
                NOW = now
            };
    }
}
