using System.Threading.Tasks;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDeletePublishedProvidersService
    {
        Task QueueDeletePublishedProvidersJob(string fundingStreamId,
            string fundingPeriodId,
            Reference user,
            string correlationId);
    }
}