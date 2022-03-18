using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Processing.Interfaces;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeletePublishedProvidersService : IJobProcessingService
    {
        Task<Job> QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            string correlationId);
    }
}