﻿using System.Collections.Generic;

namespace CalculateFunding.Models.Jobs
{
    public class JobCreateModel
    {
        /// <summary>
        /// Required Job Type (from list of known jobs)
        /// </summary>
        public string JobType { get; set; }

        public string InvokerUserId { get; set; }

        public string InvokerUserName { get; set; }

        public string InvokerUserDisplayName { get; set; }

        public int? ItemCount { get; set; }

        public string SpecificationId { get; set; }

        public string TriggerMessage { get; set; }

        public string TriggerEntityId { get; set; }

        public string TriggerEntityType { get; set; }

        /// <summary>
        /// Optional Parent Job Id
        /// </summary>
        public string ParentJobId { get; set; }

        /// <summary>
        /// App Insights Correlation Id
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Job properties (also set as message bus properties)
        /// </summary>
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Optional message bus body
        /// </summary>
        public string MessageBody { get; set; }
    }
}
