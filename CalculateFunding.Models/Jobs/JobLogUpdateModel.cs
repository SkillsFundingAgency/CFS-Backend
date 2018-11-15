using System;

namespace CalculateFunding.Models.Jobs
{
    public class JobLogUpdateModel
    {
        /// <summary>
        /// Randomly generated GUID for ID
        /// </summary>
        public string JobLogId { get; set; }

        public int? ItemsProcessed { get; set; }

        public int? ItemsSucceeded { get; set; }

        public int? ItemsFailed { get; set; }

        /// <summary>
        /// Summary string of job log outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        public string Outcome { get; set; }

        public bool? CompletedSuccessfully { get; set; }

        public DateTimeOffset Started { get; set; }

        public DateTimeOffset Finished { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
