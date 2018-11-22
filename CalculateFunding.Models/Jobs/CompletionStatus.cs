namespace CalculateFunding.Models.Jobs
{
    public enum CompletionStatus
    {
        /// <summary>
        /// Job ran successfully
        /// </summary>
        Succeeded,
        /// <summary>
        /// Job ran with errors
        /// </summary>
        Failed,
        /// <summary>
        /// User cancelled the job from the UI
        /// </summary>
        Cancelled,
        /// <summary>
        /// The timeout period expired and will never be run (due to errors)
        /// </summary>
        TimedOut,
        /// <summary>
        /// Another job has started which supersedes this job
        /// </summary>
        Superseded,
    }
}
