namespace CalculateFunding.Models.Jobs
{
    public enum CompletionStatus
    {
        Succeeded, // Job ran successfully
        Failed, // Job ran with errors
        Cancelled, // User cancelled the job from the UI
        TimedOut, // The timeout period expired and will never be run (due to errors)
        Superseded, // Another job has started which supercedes this job
    }
}
