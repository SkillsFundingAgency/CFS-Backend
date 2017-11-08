using System;

namespace Allocations.Models
{
    public class CalculationResult
    {
        public CalculationResult(string productName, decimal value)
        {
            ProductName = productName;
            Value = value;
        }

        public CalculationResult(Exception exception)
        {
            Exception = exception;
        }

        public string ProductName { get; }
        public decimal Value { get; }

        public Exception Exception { get; }
    }
}