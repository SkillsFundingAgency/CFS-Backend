using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeletePublishedFundingBlobDocumentsService
    {
        Task DeletePublishedFundingBlobDocuments(string fundingStreamId, string fundingPeriodId, string containerName);
    }
}