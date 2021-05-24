using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingTemplateDataContext : BlobBulkDataContext
    {
        public FundingTemplateDataContext(IConfiguration configuration) 
            : base(configuration, 
                  "fundingtemplates",
                  "CalculateFunding.Api.Policy.IntegrationTests.Resources.FundingTemplateTemplate",
                  typeof(FundingTemplateDataContext).Assembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData, string now) =>
            new
            {
                ID = documentData.Id,
                FUNDINGPERIODID = documentData.FundingPeriodId,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                FUNDINGSTREAMNAME = documentData.FundingStreamName,
                TEMPLATEVERSION = documentData.TemplateVersion,
                FUNDINGVERSION = documentData.FundingVersion,
                NOW = now
            };

        protected override string GetBlobName(JsonDocument document)
        {
            var fundingStreamId = GetElement(GetFundingStream(document), "code").ToString();
            var fundingPeriodId = GetElement(GetFundingPeriod(document), "id").ToString();
            var templateVersion = GetElement(GetFunding(document), "templateVersion").ToString();

            return $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";
        }

        private JsonElement GetFunding(JsonDocument document) => GetElement(document.RootElement, "funding");
        private JsonElement GetFundingStream(JsonDocument document) => GetElement(GetFunding(document), "fundingStream");
        private JsonElement GetFundingPeriod(JsonDocument document) => GetElement(GetFunding(document), "fundingPeriod");
    }
}
