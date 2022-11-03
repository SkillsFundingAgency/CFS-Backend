using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoJobCreation
    {
        Task<Job> CreateJob(string forCorrelationId,
            string specificationId,
            bool isHardDelete,
            Reference user,
            string correlationId,
            string apiVersion,
            List<string> channelCodes);
    }
}