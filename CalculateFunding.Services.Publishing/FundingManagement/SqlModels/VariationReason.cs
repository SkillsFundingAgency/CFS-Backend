using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels
{
    public class VariationReason
    {
        [ExplicitKey]
        public int VariationReasonId { get; set; }

        public string VariationReasonName { get; set; }

        public string VariationReasonCode { get; set; }
    }
}
