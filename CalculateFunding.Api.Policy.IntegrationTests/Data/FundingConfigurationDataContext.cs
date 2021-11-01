using Microsoft.Extensions.Configuration;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationDataContext : PolicyCollectionDataContext
    {
        public FundingConfigurationDataContext(IConfiguration configuration) :
            base(configuration, "CalculateFunding.Api.Policy.IntegrationTests.Resources.FundingConfigurationTemplate")
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                FUNDINGPERIODID = documentData.FundingPeriodId,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                DEFAULTTEMPLATEVERSION = documentData.DefaultTemplateVersion,
                ALLOWEDPUBLISHEDFUNDINGSTREAMIDSTOREFERENCE = ((string[])documentData.AllowedPublishedFundingStreamsIdsToReference).AsJson(),
                NOW = now,
                RELEASEMANAGEMENTVARIATIONS = ((FundingVariation[])documentData.ReleaseManagementVariations).AsJson(),
                RELEASECHANNELS = ((FundingConfigurationChannel[])documentData.ReleaseChannels).AsJson(),
                RELEASEACTIONGROUPS = ((ReleaseActionGroup[])documentData.ReleaseActionGroups).AsJson()
            };
    }
}
