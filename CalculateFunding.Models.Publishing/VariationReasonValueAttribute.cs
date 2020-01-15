using System;

namespace CalculateFunding.Models.Publishing
{
    [AttributeUsage(AttributeTargets.Property)]
    public class VariationReasonValueAttribute : Attribute
    {
        public VariationReasonValueAttribute(VariationReason value)
        {
            Value = value;
        }

        public VariationReason Value { get; set; }
    }
}