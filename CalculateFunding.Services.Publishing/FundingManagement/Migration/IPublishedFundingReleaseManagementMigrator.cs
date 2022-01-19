using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public interface IPublishedFundingReleaseManagementMigrator
    {
        Task Migrate(Dictionary<string, FundingStream> fundingStreams, Dictionary<string, FundingPeriod> fundingPeriods, Dictionary<string, Channel> channels, Dictionary<string, SqlModels.GroupingReason> _groupingReasons, Dictionary<string, VariationReason> _variationReasons, Dictionary<string, Specification> _specifications, Dictionary<string, ReleasedProvider> releasedProviders, Dictionary<string, ReleasedProviderVersion> releasedProviderVersions);
    }
}