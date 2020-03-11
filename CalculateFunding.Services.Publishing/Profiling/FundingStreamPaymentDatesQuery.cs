using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingStreamPaymentDatesQuery : IFundingStreamPaymentDatesQuery
    {
        private readonly Policy _resilience;
        private readonly IFundingStreamPaymentDatesRepository _fundingStreamPaymentDates;

        public FundingStreamPaymentDatesQuery(IFundingStreamPaymentDatesRepository fundingStreamPaymentDates,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(fundingStreamPaymentDates, nameof(fundingStreamPaymentDates));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingStreamPaymentDatesRepository, nameof(resiliencePolicies.FundingStreamPaymentDatesRepository));
            
            _fundingStreamPaymentDates = fundingStreamPaymentDates;
            _resilience = resiliencePolicies.FundingStreamPaymentDatesRepository;
        }

        public async Task<IActionResult> GetFundingStreamPaymentDates(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            FundingStreamPaymentDates fundingStreamPaymentDates = await _resilience.ExecuteAsync(() =>
                _fundingStreamPaymentDates.GetUpdateDates(fundingStreamId, fundingPeriodId));

            return fundingStreamPaymentDates == null ? (IActionResult)new NotFoundResult() : 
                new OkObjectResult(fundingStreamPaymentDates);
        }
    }
}