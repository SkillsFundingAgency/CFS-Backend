using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Specs
{
    public static class ExtensionMethods
    {
        public static Policy GetPolicy(this Policy policy, string id)
        {         
            if (policy.Id == id) return policy;
            return policy.SubPolicies?.FirstOrDefault(x => x.GetPolicy(id) != null);
        }

        public static Policy GetPolicy(this SpecificationVersion specification, string id)
        {
            foreach (Policy policy in specification.Policies)
            {
                if (policy.Id == id)
                    return policy;

                if (policy.SubPolicies != null)
                {
                    foreach (Policy subPolicy in policy.SubPolicies)
                    {
                        if (subPolicy.Id == id)
                            return subPolicy;
                    }
                }
            }
            return null;
        }

        public static Policy GetPolicyByName(this Policy policy, string name)
        {
            if (policy.Name == name) return policy;
            return policy.SubPolicies?.FirstOrDefault(x => x.GetPolicyByName(name) != null);
        }

        public static Policy GetPolicyByName(this SpecificationVersion specification, string name)
        {
            return specification.Policies?.FirstOrDefault(x => x.GetPolicyByName(name) != null);
        }

        public static IEnumerable<Calculation> GetCalculations(this SpecificationVersion specification)
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

        public static IEnumerable<Calculation> GetCalculations(this SpecificationCurrentVersion specification)
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

        public static IEnumerable<Calculation> GetCalculations(this Policy policy)
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

        public static Calculation GetCalculation(this Policy policy, string id)
        {
            return policy.Calculations?.FirstOrDefault(x => x.Id == id);
        }

        public static Calculation GetCalculationByName(this Policy policy, string name)
        {
            return policy.Calculations?.FirstOrDefault(x => x.Name == name);
        }

        public static IEnumerable<Calcs.Calculation> GenerateCalculations(this SpecificationVersion specification)
        {
            foreach (var subPolicy in specification.Policies)
            {
                foreach (var calculationSpecification in subPolicy.GenerateCalculations(specification))
                {
                    yield return calculationSpecification;
                }
            }
        }

        public static IEnumerable<Calcs.Calculation> GenerateCalculations(this Policy policy, SpecificationVersion specification, List<Reference> parentPolicySpecifications = null)
        {
            var policies = (parentPolicySpecifications ?? new List<Reference>()).Concat(new[] { policy.GetReference() }).ToList();
            if (policy.Calculations != null)
            {
                foreach (var calculationSpecification in policy.Calculations)
                {
                    yield return new Calcs.Calculation
                    {
                        Id = Reference.NewId(),
                        Name = calculationSpecification.Name,
                        CalculationSpecification = calculationSpecification,
                        AllocationLine = calculationSpecification.AllocationLine,
                        Policies = policies
                    };
                }
            }
            if (policy.SubPolicies != null)
            {
                foreach (var subPolicy in policy.SubPolicies)
                {
                    foreach (var calculationSpecification in subPolicy.GenerateCalculations(specification, policies))
                    {
                        yield return calculationSpecification;
                    }
                }
            }
        }

    }
}
