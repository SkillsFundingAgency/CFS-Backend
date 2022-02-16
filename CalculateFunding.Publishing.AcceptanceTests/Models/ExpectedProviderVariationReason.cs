using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    public class ExpectedProviderVariationReason
    {
        public int ReleasedProviderChannelVariationReasonId { get; set; }

        public string VariationReason { get; set; }

        public int ReleasedProviderVersionChannelId { get; set; }
    }
}
