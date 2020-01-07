using System;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Jobs
{
    /// <summary>
    /// Job log - one or more of these will be reported into the job service to form a log of status updates
    /// Persisted in cosmos and used to trigger status changes for a job
    /// </summary>
    public class JobLog : IIdentifiable
    {
        /// <summary>
        /// Randomly generated GUID for ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("itemsProcessed")]
        public int? ItemsProcessed { get; set; }

        [JsonProperty("itemsSucceeded")]
        public int? ItemsSucceeded { get; set; }

        [JsonProperty("itemsFailed")]
        public int? ItemsFailed { get; set; }

        /// <summary>
        /// Summary string of job log outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        [JsonProperty("outcome")]
        public string Outcome { get; set; }

        [JsonProperty("completedSuccessfully")]
        public bool? CompletedSuccessfully { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
}
