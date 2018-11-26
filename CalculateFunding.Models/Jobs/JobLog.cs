using System;

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
        public string Id { get; set; }

        public string JobId { get; set; }

        public int? ItemsProcessed { get; set; }

        public int? ItemsSucceeded { get; set; }

        public int? ItemsFailed { get; set; }

        /// <summary>
        /// Summary string of job log outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        public string Outcome { get; set; }

        public bool? CompletedSuccessfully { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}
