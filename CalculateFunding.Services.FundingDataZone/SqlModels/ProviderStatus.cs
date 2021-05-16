using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("ProviderStatus")]
    public class ProviderStatus
    {
        [ExplicitKey]
        public int ProviderStatusId { get; set; }

        public string ProviderStatusName { get; set; }
    }
}
