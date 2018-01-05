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

        public static IEnumerable<CalculationSpecification> GetCalculations(this Specification specification)
        {
            if (specification.Policies != null)
            {
                foreach (var policy in specification.Policies)
                {
                    foreach (var calculationSpecification in policy.GetCalculations())
                    {
                        yield return calculationSpecification;
                    }

                }
            }
        }

        public static IEnumerable<CalculationSpecification> GetCalculations(this PolicySpecification policy)
        {
            if (policy.Calculations != null)
            {
                foreach (var calculationSpecification in policy.Calculations)
                {
                    yield return calculationSpecification;
                }
            }
            if (policy.SubPolicies != null)
            {
                foreach (var subPolicy in policy.SubPolicies)
                {
                    foreach (var calculationSpecification in subPolicy.GetCalculations())
                    {
                        yield return calculationSpecification;
                    }
                }
            }
        }

        public static CalculationSpecification GetCalculation(this PolicySpecification policy, string id)
        {
            return policy.Calculations?.FirstOrDefault(x => x.Id == id);
        }
    }
}
