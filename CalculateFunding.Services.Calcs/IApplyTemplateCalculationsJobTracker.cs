using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public interface IApplyTemplateCalculationsJobTracker
    {
        Task<bool> TryStartTrackingJob();
        
        Task NotifyProgress(int itemCount);

        Task CompleteTrackingJob(string outcome, int itemCount);
        
        Task FailJob(string outcome);
        string JobId { get; }
        IJobsApiClient Jobs { get; }
        AsyncPolicy JobsResiliencePolicy { get; }
        ILogger Logger { get; }
    }
}