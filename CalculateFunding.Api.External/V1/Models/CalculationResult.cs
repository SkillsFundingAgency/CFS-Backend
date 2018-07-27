using System;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    public class CalculationResult
    {
        public CalculationResult()
        {
        }

        public CalculationResult(string calculationName, ushort calculationVersionNumber, string calculationStatus, decimal calculationAmount)
        {
            CalculationName = calculationName;
            CalculationVersionNumber = calculationVersionNumber;
            CalculationStatus = calculationStatus;
            CalculationAmount = calculationAmount;
        }

        public string CalculationName { get; set; }

        public ushort CalculationVersionNumber { get; set; }

        public string CalculationStatus { get; set; }

        public decimal CalculationAmount { get; set; }
    }
}