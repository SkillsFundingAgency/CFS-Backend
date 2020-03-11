using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingStreamPaymentDatesRepository
    {
        Task SaveFundingStreamUpdatedDates(FundingStreamPaymentDates paymentDates);
        Task<FundingStreamPaymentDates> GetUpdateDates(string fundingStreamId, string fundingPeriodId);
    }
}