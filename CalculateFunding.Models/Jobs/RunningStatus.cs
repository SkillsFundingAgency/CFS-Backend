namespace CalculateFunding.Models.Jobs
{
    public enum RunningStatus
    {
        Queued, // Created and waiting to be actioned
        QueuedWithService, // Sent to the microservice to action
        InProgress, // Job is running
        Completed, // Job has completed
    }
}
