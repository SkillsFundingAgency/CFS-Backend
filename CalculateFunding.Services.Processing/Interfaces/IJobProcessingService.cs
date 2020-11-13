using CalculateFunding.Common.ApiClient.Jobs.Models;

namespace CalculateFunding.Services.Processing.Interfaces
{
    public interface IJobProcessingService : IProcessingService
    {
        JobViewModel Job { get; }
    }
}
