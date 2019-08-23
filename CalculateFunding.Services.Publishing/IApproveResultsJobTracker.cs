using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public interface IApproveResultsJobTracker
    {
        Task<bool> TryStartTrackingJob(string jobId);
        Task CompleteTrackingJob(string jobId);
    }
}