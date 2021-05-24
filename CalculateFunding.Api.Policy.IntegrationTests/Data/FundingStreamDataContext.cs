using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingStreamDataContext : PolicyCollectionDataContext
    {
        public FundingStreamDataContext(IConfiguration configuration) :
            base(configuration, "CalculateFunding.Api.Policy.IntegrationTests.Resources.FundingStreamTemplate")
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                SHORTNAME = documentData.ShortName,
                NAME = documentData.Name,
                NOW = now
            };
    }
}
