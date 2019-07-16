using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.Specs
{
    public static class ExtensionMethods
    {
        public static Calculation GetCalculation(this Policy policy, string id)
        {
            return policy.Calculations?.FirstOrDefault(x => x.Id == id);
        }

        public static Calculation GetCalculationByName(this Policy policy, string name)
        {
            return policy.Calculations?.FirstOrDefault(x => x.Name == name);
        }
    }
}
