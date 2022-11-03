using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoJobService : IJobProcessingService
    {
        Task<Job> QueueJob(string forCorrelationId,
            string specificationId,
            bool isHardDelete,
            Reference user,
            string correlationId, 
            string apiVersion,
            List<string> channelCodes);
    }
}