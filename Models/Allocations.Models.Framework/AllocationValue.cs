namespace Allocations.Models.Framework
{
    public class AllocationValue
    {
        public decimal? Value { get; }

        public ProductValueType ValueType { get; }

        public AllocationValue(decimal decimalValue)
        {
            ValueType = ProductValueType.Money;
            Value = decimalValue;
        }
    }
}