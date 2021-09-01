using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class ReleaseManagementImportContext : IReleaseManagementImportContext
    {
        public Dictionary<string, FundingStream> FundingStreams { get; set; }
        public Dictionary<string, FundingPeriod> FundingPeriods { get; set; }

        public ICosmosDbFeedIterator Documents { get; set; }
        public Dictionary<string, Channel> Channels { get; set; }
        public Dictionary<string, SqlModels.GroupingReason> GroupingReasons { get; set; }
        public Dictionary<string, VariationReason> VariationReasons { get; set; }
        public Dictionary<string, Specification> Specifications { get; set; }
        public string JobId { get; set; }
    }
}
