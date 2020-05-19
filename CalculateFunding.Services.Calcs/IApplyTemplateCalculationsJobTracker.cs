using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
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
        IJobManagement Jobs { get; }
        ILogger Logger { get; }
    }
}