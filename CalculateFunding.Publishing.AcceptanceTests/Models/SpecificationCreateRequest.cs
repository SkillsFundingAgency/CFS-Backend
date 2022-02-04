using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class SpecificationCreateRequest
    {
        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }
    }
}
