using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationCreateModel
    {
        public string AcademicYearId { get; set; }

        public string FundingStreamId { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }
    }
}
