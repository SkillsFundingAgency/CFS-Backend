﻿using System;
using System.Collections.Generic;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class DatasetSearchResult
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<string> PeriodNames { get; set; }

        public string DefinitionName { get; set; }

        public string Status { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public IEnumerable<string> SpecificationNames { get; set; }

        public int Version { get; set; }

        public string ChangeNote { get; set; }

        public string LastUpdatedByName { get; set; }

        public string LastUpdatedById { get; set; }
        public string FundingStreamId { get; set; }
        public string FundingStreamName { get; set; }

    }
}
