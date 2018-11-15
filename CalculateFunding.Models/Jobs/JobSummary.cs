namespace CalculateFunding.Models.Jobs
{
    public class JobSummary
    {
        /// <summary>
        /// Required - randomly generated GUID for this job ID
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Required Job Definition (from list of known jobs)
        /// </summary>
        public string JobDefinitionId { get; set; }

        public RunningStatus RunningStatus { get; set; }

        public CompletionStatus? CompletionStatus { get; set; }

        public string InvokerUserId { get; set; }

        public string InvokerUserDisplayName { get; set; }

        /// <summary>
        /// Optional Parent Job Id
        /// </summary>
        public string ParentJobId { get; set; }
    }
}
