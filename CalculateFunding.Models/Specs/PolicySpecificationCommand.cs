namespace CalculateFunding.Models.Specs
{
    public class PolicySpecificationCommand : Command<PolicySpecification>
    {
        public string SpecificationId { get; set; }
        public string ParentPolicyId { get; set; }
    }
}