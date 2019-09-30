using System;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedFundingDates
    {
        public DateTime StatusChangedDate { get; set; }

        public DateTime ExternalPublicationDate { get; set; }

        public DateTime EarliestPaymentAvailableDate { get; set; }
    }
}
