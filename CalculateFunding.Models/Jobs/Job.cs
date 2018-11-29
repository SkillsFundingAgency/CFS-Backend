using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job entity - current version to be persisted into Cosmos
    /// </summary>
    public class Job : IIdentifiable
    {
        /// <summary>
        /// Required - randomly generated GUID for this job ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Required Job Definition (from list of known jobs)
        /// </summary>
        public string JobDefinitionId { get; set; }

        public RunningStatus RunningStatus { get; set; }

        public CompletionStatus? CompletionStatus { get; set; }

        public string InvokerUserId { get; set; }

        public string InvokerUserDisplayName { get; set; }

        public int? ItemCount { get; set; }

        /// <summary>
        /// Specification ID job relates to. This is required based on the Job Definition
        /// </summary>
        public string SpecificationId { get; set; }

        public Trigger Trigger { get; set; }

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

        /// <summary>
        /// Date and time job was last updated
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; }
    }
}
