using System;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationPublishDateModel
    {
        public DateTimeOffset? ExternalPublicationDate { get; set; }

        public DateTimeOffset? EarliestPaymentAvailableDate { get; set; }
    }
}
