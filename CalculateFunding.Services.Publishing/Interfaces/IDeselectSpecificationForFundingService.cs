using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeselectSpecificationForFundingService
    {
        Task DeselectSpecificationForFunding(string fundingStreamId, string fundingPeriodId);
    }
}