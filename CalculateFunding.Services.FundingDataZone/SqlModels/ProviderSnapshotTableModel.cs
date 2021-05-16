using System;
using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("ProviderSnapshot")]
    public class ProviderSnapshotTableModel
    {
        [Key]
        public int ProviderSnapshotId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Version { get; set; }

        public DateTime TargetDate { get; set; }

        public DateTime Created { get; set; }

        public int FundingStreamId { get; set; }
    }
}
