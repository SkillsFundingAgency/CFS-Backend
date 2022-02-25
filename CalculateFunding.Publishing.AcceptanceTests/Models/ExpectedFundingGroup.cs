﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ExpectedFundingGroup
    {
        public int FundingGroupId { get; set; }

        public string SpecificationId { get; set; }

        public string Channel { get; set; }

        public string GroupingReason { get; set; }

        public string OrganisationGroupTypeCode { get; set; }

        public string OrganisationGroupTypeIdentifier { get; set; }

        public string OrganisationGroupIdentifierValue { get; set; }

        public string OrganisationGroupName { get; set; }

        public string OrganisationGroupSearchableName { get; set; }

        public string OrganisationGroupTypeClassification { get; set; }
    }
}