using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeleteFundingSearchDocumentsService
    {
        Task DeleteFundingSearchDocuments<TIndex>(string fundingStreamId, string fundingPeriodId)
            where TIndex : class;
    }
}