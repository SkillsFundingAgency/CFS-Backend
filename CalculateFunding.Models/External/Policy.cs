using System;

namespace CalculateFunding.Models.External
{
    /// <summary>
    /// Repesents a funding stream
    /// </summary>
    [Serializable]
    public class Policy
    {
        public Policy()
        {
        }

        public Policy(string policyId, string policyName, string policyDescription)
        {
            PolicyId = policyId;
            PolicyName = policyName;
            PolicyDescription = policyDescription;
        }

        /// <summary>
        /// The identifier for the policy
        /// </summary>
        public string PolicyId { get; set; }

        /// <summary>
        /// The name of the policy
        /// </summary>
        public string PolicyName { get; set; }

        /// <summary>
        /// The description of the policy
        /// </summary>
        public string PolicyDescription { get; set; }
    }
}