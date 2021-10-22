using CalculateFunding.Models.Policy;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;

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

        public FundingPeriod GetFundingPeriod(dynamic documentData, string now = null)
        {
            now ??= DateTime.UtcNow.ToString("O");
            Common.Models.DocumentEntity<FundingPeriod> documentEntity = JsonConvert.DeserializeObject<Common.Models.DocumentEntity<FundingPeriod>>(GetFormattedDocument(documentData, now));

            return documentEntity.Content;
        }
    }
}
