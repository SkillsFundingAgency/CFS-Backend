using System;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    public class ExpectedProviderVariationReason
    {
        public Guid ReleasedProviderChannelVariationReasonId { get; set; }

        public string VariationReason { get; set; }

        public Guid ReleasedProviderVersionChannelId { get; set; }
    }
}
