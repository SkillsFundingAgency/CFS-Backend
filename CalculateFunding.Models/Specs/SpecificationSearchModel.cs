using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationSearchModel : Reference
    {
        public Reference FundingStream { get; set; }

        public Reference AcademicYear { get; set; }

        public DateTime UpdatedAt { get; set; }

        public string[] DataDefinitionRelationshipIds { get; set; }
    }
}
