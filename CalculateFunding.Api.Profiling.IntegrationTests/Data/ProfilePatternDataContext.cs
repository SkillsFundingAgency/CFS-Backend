using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.IntegrationTests.Common.Data;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Profiling.IntegrationTests.Data
{
    public class ProfilePatternDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public ProfilePatternDataContext(IConfiguration configuration)
            : base(configuration,
                "profiling",
                "CalculateFunding.Api.Profiling.IntegrationTests.Resources.ProfilePatternTemplate",
                typeof(ProfilePatternDataContext).Assembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                FUNDINGPERIODID = documentData.FundingPeriodId,
                FUNDINGSTREAM = documentData.FundingStream,
                FUNDINGLINEID = documentData.FundingLineId,
                FUNDINGSTREAMPERIODSTARTDATE = documentData.FundingStreamPeriodStartDate,
                FUNDINGSTREAMPERIODENDDATE = documentData.FundingStreamPeriodEndDate,
                PROFILEPATTERN = ((ProfilePeriodPattern[]) documentData.ProfilePattern).AsJson().Prettify(),
                DISPLAYNAME = documentData.DisplayName,
                REPROFILINGENABLED = documentData.ReProfilingEnabled,
                INCREASEDAMOUNTSTRATEGYKEY = documentData.IncreasedAmountStrategyKey,
                DECREASEDAMOUNTSTRATEGYKEY = documentData.DecreasedAmountStrategyKey,
                SAMEAMOUNTSTRATEGYKEY = documentData.SameAmountStrategyKey,
                NOW = now
            };
    }
}