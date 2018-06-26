namespace CalculateFunding.Models.Specs
{
    public class PolicySpecificationCommand : Command<Policy>
    {
        public string SpecificationId { get; set; }
        public string ParentPolicyId { get; set; }
    }
}