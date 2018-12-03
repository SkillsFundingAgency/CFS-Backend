using System;

namespace CalculateFunding.Models.Jobs
{
    public class JobSummary
    {
        public string JobId { get; set; }

        public string JobType { get; set; }

        public string SpecificationId { get; set; }

        public string EntityId { get; set; }

        public RunningStatus RunningStatus { get; set; }

        public CompletionStatus? CompletionStatus { get; set; }

        public string InvokerUserId { get; set; }

        public string InvokerUserDisplayName { get; set; }

        public string ParentJobId { get; set; }

        public DateTimeOffset LastUpdated { get; set; }
    }
}
