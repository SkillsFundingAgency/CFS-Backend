using System.Collections.Generic;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    public class ExpectedVariationReasons
    {
        public string ProviderId { get; set; }

        public IEnumerable<VariationReason> Reasons { get; set; } = new VariationReason[0];
    }
}