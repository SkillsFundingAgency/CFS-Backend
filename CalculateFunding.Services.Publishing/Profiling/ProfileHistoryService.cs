using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ProfileHistoryService : IProfileHistoryService
    {
        private readonly IFundingStreamPaymentDatesRepository _paymentDates;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly AsyncPolicy _publishedFundingPolicy;
        private readonly AsyncPolicy _paymentDatesPolicy;

        public ProfileHistoryService(IFundingStreamPaymentDatesRepository paymentDates,
            IPublishedFundingRepository publishedFunding,
            IDateTimeProvider dateTimeProvider,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(paymentDates, nameof(paymentDates));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(dateTimeProvider, nameof(dateTimeProvider));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingStreamPaymentDatesRepository, nameof(resiliencePolicies.FundingStreamPaymentDatesRepository));
            
            _paymentDates = paymentDates;
            _publishedFunding = publishedFunding;
            _dateTimeProvider = dateTimeProvider;
            _publishedFundingPolicy = resiliencePolicies.PublishedFundingRepository;
            _paymentDatesPolicy = resiliencePolicies.FundingStreamPaymentDatesRepository;
        }

        public async Task<IActionResult> GetProfileHistory(string fundingStreamId, 
            string fundingPeriodId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            FundingStreamPaymentDates paymentDates = await _paymentDatesPolicy.ExecuteAsync(() 
                => _paymentDates.GetUpdateDates(fundingStreamId, fundingPeriodId));

            PublishedProviderVersion publishedProviderVersion = await _publishedFundingPolicy.ExecuteAsync(()
                => _publishedFunding.GetLatestPublishedProviderVersion(fundingStreamId, fundingPeriodId, providerId));

            if (publishedProviderVersion == null)
            {
               return new NotFoundResult();
            }
            
            ProfileTotal[] publishedProviderProfiling = new PaymentFundingLineProfileTotals(publishedProviderVersion)
                .ToArray();
                
            foreach (FundingStreamPaymentDate paymentDate in PastPaymentDates(paymentDates))
            {
                ProfileTotal matchingProfileTotal = publishedProviderProfiling.SingleOrDefault(_ => _.Year == paymentDate.Year &&
                                                                                                    _.TypeValue == paymentDate.TypeValue &&
                                                                                                    _.Occurrence == paymentDate.Occurrence);

                if (matchingProfileTotal == null)
                {
                    continue;
                }

                matchingProfileTotal.IsPaid = true;
            }

            return new OkObjectResult(publishedProviderProfiling);
        }

        private IEnumerable<FundingStreamPaymentDate> PastPaymentDates(FundingStreamPaymentDates paymentDates)
        {
            DateTimeOffset today = _dateTimeProvider.UtcNow.Date;
            
            return (paymentDates?.PaymentDates ?? Enumerable.Empty<FundingStreamPaymentDate>()).Where(_ => _.Date <= today);
        }
    }
}