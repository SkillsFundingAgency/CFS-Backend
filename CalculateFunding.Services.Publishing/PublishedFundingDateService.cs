using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingDateService : IPublishedFundingDateService
    {
        public PublishedFundingDates GetDatesForSpecification()
        {
            DateTime now = DateTime.Now;

            return new PublishedFundingDates()
            {
                StatusChangedDate = now,
                EarliestPaymentAvailableDate = now,
                ExternalPublicationDate = now
            };
        }
    }
}
