using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job entity - current version to be persisted into Cosmos
    /// </summary>
    public class Job
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

        public string InvokerUserId { get; set; }

        public string InvokerUserName { get; set; }

        public string InvokerUserDisplayName { get; set; }

        public int? ItemCount { get; set; }

        /// <summary>
        /// Specification ID job relates to. This is required based on the Job Definition
        /// </summary>
        public string SpecificationId { get; set; }

        /// <summary>
        /// Required: Human readable message describing the trigger
        /// eg Specification data map change
        /// </summary>
        public string TriggerMessage { get; set; }

        /// <summary>
        /// Trigger entity ID, eg Calculation Specification ID
        /// Optional depending on JobType configuration.
        /// </summary>
        public string TriggerEntityId { get; set; }

        /// <summary>
        /// Trigger Entity Type
        /// eg CalculationSpecification
        /// Optional depending on JobType configuration.
        /// </summary>
        public string TriggerEntityType { get; set; }

        /// <summary>
        /// Optional Parent Job Id
        /// </summary>
        public string ParentJobId { get; set; }

        /// <summary>
        /// Optional Superseded Job ID
        /// </summary>
        public string SupersededByJobId { get; set; }

        /// <summary>
        /// App Insights Correlation Id
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Job properties (also set as message bus properties)
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Optional service bus message bus body
        /// </summary>
        public string MessageBody { get; set; }


        /// <summary>
        /// Date and Time Job created
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Date and time job finished (to be set when CompletionStatus is set)
        /// </summary>
        public DateTimeOffset? Completed { get; set; }

        /// <summary>
        /// Summary string of job outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        public string Outcome { get; set; }
    }
}
