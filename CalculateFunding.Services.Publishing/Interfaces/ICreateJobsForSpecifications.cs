using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICreateJobsForSpecifications
    {
        Task<Job> CreateJob(string specificationId,
            Reference user,
            string correlationId,
            Dictionary<string, string> properties = null,
            string messageBody = null,
            string parentJobId = null);
        
        string JobDefinitionId { get; }
        
        string TriggerMessage { get; }
    }
}