using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job definition (loading from config source at runtime) describing a particular job type
    /// A job instance must reference a job type and the behaviour configured in this class will be used to transition this job and related jobs for the specification through different states
    /// </summary>
    public class JobDefinition : IIdentifiable
    {
        /// <summary>
        /// Friendly identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Optional - enqueue job message on this message bus queue
        /// </summary>
        [JsonProperty("messageBusQueue")]
        public string MessageBusQueue { get; set; }

        /// <summary>
        /// Optional - enqueue job message on this message bus topic
        /// </summary>
        [JsonProperty("messageBusTopic")]
        public string MessageBusTopic { get; set; }

        /// <summary>
        /// Set any existing running jobs to superseded when a new job is queued
        /// </summary>
        [JsonProperty("supersedeExistingRunningJobOnEnqueue")]
        public bool SupersedeExistingRunningJobOnEnqueue { get; set; }

        /// <summary>
        /// Require a specification ID to be provided when job is enqueued
        /// </summary>
        [JsonProperty("requireSpecificationId")]
        public bool RequireSpecificationId { get; set; }

        /// <summary>
        /// Require an entity id when a job is enqueued
        /// </summary>
        [JsonProperty("requireEntityId")]
        public bool RequireEntityId { get; set; }

        /// <summary>
        /// Require a message body to set as service bus message body
        /// </summary>
        [JsonProperty("requireMessageBody")]
        public bool RequireMessageBody { get; set; }

        /// <summary>
        /// Require the following keys to be set and have values to be valid on job create
        /// </summary>
        [JsonProperty("requireMessageProperties")]
        public IEnumerable<string> RequireMessageProperties { get; set; }

        /// <summary>
        /// Treat the Item Count of the job as a percentage
        /// </summary>
        [JsonProperty("itemCountIsPercentage")]
        public bool ItemCountIsPercentage { get; set; }

        /// <summary>
        /// Amount of time before a running job times out and is cancelled
        /// </summary>
        [JsonProperty("timeout")]
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Message to set outcome of job to when successfully run.
        /// {0} is total item count
        /// {1} is number of items completed successfully
        /// {2} is number of items with errors
        /// </summary>
        [JsonProperty("completionOutcomeTemplateSuccess")]
        public string CompletionOutcomeTemplateSuccess { get; set; }

        /// <summary>
        /// Message to set outcome of job to when has failed
        /// {0} is total item count
        /// {1} is number of items completed successfully
        /// {2} is number of items with errors
        /// </summary>
        [JsonProperty("completionOutcomeTemplateFailure")]
        public string CompletionOutcomeTemplateFailure { get; set; }

        /// <summary>
        /// Copy the status message from the log when only a single log entry is reported and job is finished
        /// </summary>
        [JsonProperty("copyOutcomeFromLogWhenSingleLogReported")]
        public bool CopyOutcomeFromLogWhenSingleLogReported { get; set; }
    }
}
