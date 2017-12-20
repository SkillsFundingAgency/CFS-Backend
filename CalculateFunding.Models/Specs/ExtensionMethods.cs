using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public static class ExtensionMethods
    {
        public static PolicySpecification GetPolicy(this PolicySpecification policy, string id)
        {         
            if (policy.Id == id) return policy;
            return policy.SubPolicies?.FirstOrDefault(x => x.GetPolicy(id) != null);
        }
        public static PolicySpecification GetPolicy(this Specification specification, string id)
        {
            return specification.Policies?.FirstOrDefault(x => x.GetPolicy(id) != null);
        }

        public static CalculationSpecification GetCalculation(this PolicySpecification policy, string id)
        {
            return policy.Calculations?.FirstOrDefault(x => x.Id == id);
        }
    }
}
