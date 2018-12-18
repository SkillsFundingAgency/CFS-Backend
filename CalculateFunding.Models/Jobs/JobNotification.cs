using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job notification to be sent to the notification service when a jobs details change
    /// </summary>
    public class JobNotification
    {
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("jobType")]
        public string JobType { get; set; }

        [JsonProperty("runningStatus")]
        public RunningStatus RunningStatus { get; set; }

        [JsonProperty("completionStatus")]
        public CompletionStatus? CompletionStatus { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("invokerUserId")]
        public string InvokerUserId { get; set; }

        [JsonProperty("invokerUserDisplayName")]
        public string InvokerUserDisplayName { get; set; }

        [JsonProperty("itemCount")]
        public int? ItemCount { get; set; }

        [JsonProperty("overallItemsProcessed")]
        public int? OverallItemsProcessed { get; set; }

        [JsonProperty("overallItemsSucceeded")]
        public int? OverallItemsSucceeded { get; set; }

        [JsonProperty("overallItemsFailed")]
        public int? OverallItemsFailed { get; set; }

        [JsonProperty("trigger")]
        public Trigger Trigger { get; set; }

        /// <summary>
        /// Optional Parent Job Id
        /// </summary>
        [JsonProperty("parentJobId")]
        public string ParentJobId { get; set; }

        /// <summary>
        /// Optional Superseded Job ID
        /// </summary>
        [JsonProperty("supersededByJobId")]
        public string SupersededByJobId { get; set; }

        [JsonProperty("statusDateTime")]
        public DateTimeOffset StatusDateTime { get; set; }

        /// <summary>
        /// Summary string of job outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        [JsonProperty("outcome")]
        public string Outcome { get; set; }
    }
}
