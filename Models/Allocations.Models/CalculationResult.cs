namespace Allocations.Models
{
    public class CalculationResult
    {
        public CalculationResult(string productName, decimal value)
        {
            ProductName = productName;
            Value = value;
        }

        public string ProductName { get; }
        public decimal Value { get; }
    }
}