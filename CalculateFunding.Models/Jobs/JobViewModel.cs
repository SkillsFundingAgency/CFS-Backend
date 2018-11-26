using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Jobs
{
    public class JobViewModel
    {
        public string Id { get; set; }

        public string JobDefinitionId { get; set; }

        public RunningStatus RunningStatus { get; set; }

        public CompletionStatus? CompletionStatus { get; set; }

        public string InvokerUserId { get; set; }

        public string InvokerUserDisplayName { get; set; }

        public int? ItemCount { get; set; }

        public string SpecificationId { get; set; }

        public Trigger Trigger { get; set; }

        public string ParentJobId { get; set; }

        public string SupersededByJobId { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public string MessageBody { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Completed { get; set; }

        public string Outcome { get; set; }

        public ICollection<JobViewModel> ChildJobs { get; } = new List<JobViewModel>();
    }
}
