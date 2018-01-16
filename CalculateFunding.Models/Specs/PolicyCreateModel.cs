using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class PolicyCreateModel
    {
        public string SpecificationId { get; set; }

        public string ParentPolicyId { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }
    }
}
