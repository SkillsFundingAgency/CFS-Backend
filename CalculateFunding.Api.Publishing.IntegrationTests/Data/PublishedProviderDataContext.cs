using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Publishing.IntegrationTests.Data
{
    public class PublishedProviderDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public PublishedProviderDataContext(IConfiguration configuration)
            : base(configuration,
                "publishedfunding",
                "CalculateFunding.Api.Publishing.IntegrationTests.Resources.PublishedProviderTemplate",
                typeof(PublishedProviderDataContext).Assembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                SPECIFICATIONID = documentData.SpecificationId,
                PUBLISHEDPROVIDERID = documentData.PublishedProviderId,
                FUNDINGSTREAM = documentData.FundingStream, 
                TOTALFUNDING = documentData.TotalFunding,
                PROVIDERTYPE = documentData.ProviderType,
                PROVIDERSUBTYPE = documentData.ProviderSubType,
                LACODE = documentData.LaCode,
                ISINDICATIVE = documentData.IsIndicative,
                STATUS = documentData.Status,
                NOW = now
            };
    }
}
