using System;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class CalculationResult
    {
        public CalculationResult()
        {
        }

        public CalculationResult(string calculationName, ushort calculationVersionNumber, string calculationType, decimal calculationValue, string policyId, bool associatedWithAllocation)
        {
            CalculationName = calculationName;
            CalculationVersionNumber = calculationVersionNumber;
            CalculationType = calculationType;
            CalculationValue = calculationValue;
	        PolicyId = policyId;
	        AssociatedWithAllocation = associatedWithAllocation;
        }

        public string CalculationName { get; set; }

	    public string CalculationDisplayName { get; set; }

	    public string CalculationType { get; set; }

	    public decimal CalculationValue { get; set; }

	    public string PolicyId { get; set; }

		public ushort CalculationVersionNumber { get; set; }

	    public bool AssociatedWithAllocation { get; set; }

    }
}