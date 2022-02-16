using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingDateService : IPublishedFundingDateService
    {
        private readonly ICurrentDateTime _currentDateTime;

        public PublishedFundingDateService(ICurrentDateTime currentDateTime)
        {
            _currentDateTime = currentDateTime;
        }

        public PublishedFundingDates GetDatesForSpecification()
        {
            DateTime now = _currentDateTime.GetUtcNow();

            return new PublishedFundingDates()
            {
                StatusChangedDate = now,
                EarliestPaymentAvailableDate = now,
                ExternalPublicationDate = now
            };
        }
    }
}
