using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoJobCreation
    {
        Task<Job> CreateJob(string forCorrelationId,
            bool isHardDelete,
            Reference user,
            string correlationId);
    }
}