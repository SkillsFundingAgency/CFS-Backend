using System;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job notification to be sent to the notification service when a jobs details change
    /// </summary>
    public class JobNotification
    {
        /// <summary>
        /// Required - randomly generated GUID for this job ID
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Required Job Type (from list of known jobs)
        /// </summary>
        public string JobType { get; set; }

        public RunningStatus RunningStatus { get; set; }

        public CompletionStatus? CompletionStatus { get; set; }

        public string SpecificationId { get; set; }

        public string InvokerUserId { get; set; }

        public string InvokerUserDisplayName { get; set; }

        public int? ItemCount { get; set; }

        public int? OverallItemsProcessed { get; set; }

        public int? OverallItemsSucceeded { get; set; }

        public int? OverallItemsFailed { get; set; }

        public Trigger Trigger { get; set; }

        /// <summary>
        /// Optional Parent Job Id
        /// </summary>
        public string ParentJobId { get; set; }

        /// <summary>
        /// Optional Superseded Job ID
        /// </summary>
        public string SupersededByJobId { get; set; }

        public DateTimeOffset StatusDateTime { get; set; }

        /// <summary>
        /// Summary string of job outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        public string Outcome { get; set; }
    }
}
