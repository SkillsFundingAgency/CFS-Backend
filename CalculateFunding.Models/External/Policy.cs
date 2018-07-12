namespace CalculateFunding.Models.External
{
    public class Policy
    {
        public Policy(string policyId, string policyName, string policyDescription)
        {
            PolicyId = policyId;
            PolicyName = policyName;
            PolicyDescription = policyDescription;
        }

        public string PolicyId { get; set; }

        public string PolicyName { get; set; }

        public string PolicyDescription { get; set; }
    }
}