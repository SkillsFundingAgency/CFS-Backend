using System;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    public class CalculationResult
    {
        public CalculationResult()
        {
        }

        public CalculationResult(string calculationName, ushort calculationVersionNumber, string calculationType, decimal calculationAmount)
        {
            CalculationName = calculationName;
            CalculationVersionNumber = calculationVersionNumber;
            CalculationType = CalculationType;
            CalculationAmount = calculationAmount;
        }

        public string CalculationName { get; set; }

        public ushort CalculationVersionNumber { get; set; }

        public string CalculationType { get; set; }

        public decimal CalculationAmount { get; set; }
    }
}