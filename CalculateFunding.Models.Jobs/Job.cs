using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

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
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("jobId")]
        public string JobId
        {
            get
            {
                return Id;
            }
        }

        /// <summary>
        /// Required Job Definition (from list of known jobs)
        /// </summary>
        [JsonProperty("jobDefinitionId")]
        public string JobDefinitionId { get; set; }

        [JsonProperty("runningStatus")]
        public RunningStatus RunningStatus { get; set; }

        [JsonProperty("completionStatus")]
        public CompletionStatus? CompletionStatus { get; set; }

        [JsonProperty("invokerUserId")]
        public string InvokerUserId { get; set; }

        [JsonProperty("invokerDisplayName")]
        public string InvokerUserDisplayName { get; set; }

        [JsonProperty("itemCount")]
        public int? ItemCount { get; set; }

        /// <summary>
        /// Specification ID job relates to. This is required based on the Job Definition
        /// </summary>
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

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

        /// <summary>
        /// App Insights Correlation Id
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Job properties (also set as message bus properties)
        /// </summary>
        [JsonProperty("properties")]
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Optional service bus message bus body
        /// </summary>
        [JsonProperty("messageBody")]
        public string MessageBody { get; set; }


        /// <summary>
        /// Date and Time Job created
        /// </summary>
        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Date and time job finished (to be set when CompletionStatus is set)
        /// </summary>
        [JsonProperty("completed")]
        public DateTimeOffset? Completed { get; set; }

        /// <summary>
        /// Summary string of job outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        [JsonProperty("outcome")]
        public string Outcome { get; set; }

        /// <summary>
        /// Date and time job was last updated
        /// </summary>
        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}
