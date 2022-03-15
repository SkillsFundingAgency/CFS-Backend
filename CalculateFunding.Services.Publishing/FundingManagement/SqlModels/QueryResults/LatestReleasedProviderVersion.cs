using System;

namespace CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults
{
    public class LatestReleasedProviderVersion
    {
        public Guid ReleasedProviderVersionId { get; set; }
        public int LatestMajorVersion { get; set; }
        public string ProviderId { get; set; }
    }
}
