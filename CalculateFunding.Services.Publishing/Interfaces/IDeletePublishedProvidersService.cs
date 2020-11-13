using System.Threading.Tasks;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeletePublishedProvidersService : IJobProcessingService
    {
        Task QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            string correlationId);
    }
}