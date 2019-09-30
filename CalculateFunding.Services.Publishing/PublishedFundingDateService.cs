using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingDateService : IPublishedFundingDateService
    {
        private Dictionary<string, PublishedFundingDates> _dates = new Dictionary<string, PublishedFundingDates>();

        public Task<PublishedFundingDates> GetDatesForSpecification(string specificationId)
        {
            PublishedFundingDates result;
            if (!_dates.TryGetValue(specificationId, out result))
            {
                // Return the current time until the API is ready to retrieve this from the specification microservice
                DateTime dateTime = DateTime.Now;
                result = new PublishedFundingDates()
                {
                    StatusChangedDate = dateTime,
                    EarliestPaymentAvailableDate = dateTime,
                    ExternalPublicationDate = dateTime,
                };

                _dates[specificationId] = result;
            }
            return Task.FromResult(result);
        }

        public void SetDatesForSpecification(string specificationId, PublishedFundingDates publishedFundingDates)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(publishedFundingDates, nameof(publishedFundingDates));

            _dates[specificationId] = publishedFundingDates;
        }
    }
}
