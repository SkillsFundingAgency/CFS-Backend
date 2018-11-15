using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job definition (loading from config source at runtime) describing a particular job type
    /// A job instance must reference a job type and the behaviour configured in this class will be used to transition this job and related jobs for the specification through different states
    /// </summary>
    public class JobDefinition
    {
        /// <summary>
        /// A unique, human readable string, eg CalculationRun, MapDataset, ChooseFunding
        /// </summary>
        public string JobDefinitionId { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Optional - enqueue job message on this message bus queue
        /// </summary>
        public string MessageBusQueue { get; set; }

        /// <summary>
        /// Optional - enqueue job message on this message bus topic
        /// </summary>
        public string MessageBusTopic { get; set; }

        /// <summary>
        /// Set any existing running jobs to superseded when a new job is queued
        /// </summary>
        public bool SupersedeExistingRunningJobOnEnqueue { get; set; }

        /// <summary>
        /// Require a specification ID to be provided when job is enqueued
        /// </summary>
        public bool RequireSpecificationId { get; set; }

        /// <summary>
        /// Require an entity id when a job is enqueued
        /// </summary>
        public bool RequireEntityId { get; set; }

        /// <summary>
        /// Require a message body to set as service bus message body
        /// </summary>
        public bool RequireMessageBody { get; set; }

        /// <summary>
        /// Require the following keys to be set and have values to be valid on job create
        /// </summary>
        public IEnumerable<string> RequireMessageProperties { get; set; }

        /// <summary>
        /// Treat the Item Count of the job as a percentage
        /// </summary>
        public bool ItemCountIsPercentage { get; set; }

        /// <summary>
        /// Amount of time before a running job times out and is cancelled
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Message to set outcome of job to when successfully run.
        /// {0} is total item count
        /// {1} is number of items completed successfully
        /// {2} is number of items with errors
        /// </summary>
        public string CompletionOutcomeTemplateSuccess { get; set; }

        /// <summary>
        /// Message to set outcome of job to when has failed
        /// {0} is total item count
        /// {1} is number of items completed successfully
        /// {2} is number of items with errors
        /// </summary>
        public string CompletionOutcomeTemplateFailure { get; set; }

        /// <summary>
        /// Copy the status message from the log when only a single log entry is reported and job is finished
        /// </summary>
        public bool CopyOutcomeFromLogWhenSingleLogReported { get; set; }

    }
}
