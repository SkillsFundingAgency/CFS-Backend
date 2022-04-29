using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Jobs
{
    public class JobViewModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("jobDefinitionId")]
        public string JobDefinitionId { get; set; }

        [JsonProperty("runningStatus")]
        public RunningStatus RunningStatus { get; set; }

        [JsonProperty("completionStatus")]
        public CompletionStatus? CompletionStatus { get; set; }

        [JsonProperty("invokerUserId")]
        public string InvokerUserId { get; set; }

        [JsonProperty("invokerUserDisplayName")]
        public string InvokerUserDisplayName { get; set; }

        [JsonProperty("itemCount")]
        public int? ItemCount { get; set; }

        [JsonProperty("specificationid")]
        public string SpecificationId { get; set; }

        [JsonProperty("trigger")]
        public Trigger Trigger { get; set; }

        [JsonProperty("parentJobid")]
        public string ParentJobId { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("supersededByJobId")]
        public string SupersededByJobId { get; set; }

        [JsonProperty("properties")]
        public IDictionary<string, string> Properties { get; set; }

        [JsonProperty("messageBody")]
        public string MessageBody { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("completed")]
        public DateTimeOffset? Completed { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty("outcome")]
        public string Outcome { get; set; }

        [JsonProperty("childJobs")]
        public ICollection<JobViewModel> ChildJobs { get; } = new List<JobViewModel>();
    }
}
