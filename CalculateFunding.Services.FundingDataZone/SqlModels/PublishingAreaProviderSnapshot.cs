using System;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    public class PublishingAreaProviderSnapshot
    {
        public int ProviderSnapshotId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Version { get; set; }

        public DateTime TargetDate { get; set; }

        public DateTime Created { get; set; }

        public string FundingStreamCode { get; set; }

        public string FundingStreamName { get; set; }

        public string FundingPeriodName { get; set; }
    }
}
