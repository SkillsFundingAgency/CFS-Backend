using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Services.Core.Interfaces.Services;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobProcessingService : IProcessingService
    {
        JobViewModel Job { get; }
    }
}
