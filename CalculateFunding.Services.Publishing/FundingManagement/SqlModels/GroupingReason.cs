using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    [Table("GroupingReasons")]
    public class GroupingReason
    {
        [ExplicitKey]
        public int GroupingReasonId { get; set; }

        public string GroupingReasonName { get; set; }

        public string GroupingReasonCode { get; set; }
    }
}
