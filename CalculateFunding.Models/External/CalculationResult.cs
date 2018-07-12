namespace CalculateFunding.Models.External
{
    public class CalculationResult
    {
        public CalculationResult(string calculationName, int calculationVersionNumber, string calculationStatus, double calculationAmount)
        {
            CalculationName = calculationName;
            CalculationVersionNumber = calculationVersionNumber;
            CalculationStatus = calculationStatus;
            CalculationAmount = calculationAmount;
        }

        public string CalculationName { get; set; }

        public int CalculationVersionNumber { get; set; }

        public string CalculationStatus { get; set; }

        public double CalculationAmount { get; set; }
    }
}