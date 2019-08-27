using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public interface IJobTracker
    {
        Task<bool> TryStartTrackingJob(string jobId, string jobType);
        Task CompleteTrackingJob(string jobId);
        Task FailJob(string outcome, string jobId);
        Task NotifyProgress(int itemCount, string jobId);
    }
}