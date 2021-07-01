using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryFundingStreamPaymentDatesRepository
        : IFundingStreamPaymentDatesRepository
    {
        readonly IDictionary<string, FundingStreamPaymentDates> _fundingStreamPaymentDates = new Dictionary<string, FundingStreamPaymentDates>();

        public Task<FundingStreamPaymentDates> GetUpdateDates(string fundingStreamId, string fundingPeriodId)
        {
            _fundingStreamPaymentDates.TryGetValue($"{fundingStreamId}-{fundingPeriodId}", out FundingStreamPaymentDates fundingStreamPaymentDate);

            return Task.FromResult(fundingStreamPaymentDate);
        }

        public Task SaveFundingStreamUpdatedDates(FundingStreamPaymentDates paymentDates)
        {
            _fundingStreamPaymentDates[$"{paymentDates.FundingStreamId}-{paymentDates.FundingPeriodId}"] = paymentDates;

            return Task.FromResult(Task.CompletedTask);
        }
    }
}
