using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeletePublishedProvidersService
    {
        Task QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            string correlationId);

        Task DeletePublishedProvidersJob(Message message);
    }
}