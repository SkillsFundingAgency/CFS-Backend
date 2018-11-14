namespace CalculateFunding.Models.Jobs
{
    public enum RunningStatus
    {
        Queued,
        InProgress,
        Cancelled,
        Timeout,
        Complete,
        Superseded,
    }
}
