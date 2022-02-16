﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ExpectedFundingGroupVersion
    {
        public int FundingGroupVersionId { get; set; }

        public int FundingGroupId { get; set; }

        public string Channel { get; set; }

        public string GroupingReason { get; set; }

        public DateTime StatusChangedDate { get; set; }

        public int MajorVersion { get; set; }

        public int MinorVersion { get; set; }

        public string TemplateVersion { get; set; }

        public string SchemaVersion { get; set; }

        public string JobId { get; set; }

        public string CorrelationId { get; set; }

        public int FundingStreamId { get; set; }

        public int FundingPeriodId { get; set; }

        public string FundingId { get; set; }

        public decimal TotalFunding { get; set; }

        public DateTime ExternalPublicationDate { get; set; }

        public DateTime EarliestPaymentAvailableDate { get; set; }
    }
}
