﻿using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Scenarios
{
    public class CurrentTestScenario : Reference
    {
        public string Description { get; set; }

        public DateTimeOffset? LastUpdatedDate { get; set; }

        public int Version { get; set; }

        public DateTimeOffset? CurrentVersionDate { get; set; }

        public Reference Author { get; set; }

        public string Comment { get; set; }

        public PublishStatus PublishStatus { get; set; }

        public string Gherkin { get; set; }

        public string SpecificationId { get; set; }
    }
}


