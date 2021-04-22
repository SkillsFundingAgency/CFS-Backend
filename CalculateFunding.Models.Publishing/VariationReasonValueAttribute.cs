using System;

namespace CalculateFunding.Models.Publishing
{
    [AttributeUsage(AttributeTargets.Property)]
    public class VariationReasonValueAttribute : Attribute
    {
        public VariationReasonValueAttribute(VariationReason value, string[] applicableSchemaVersions = null)
        {
            Value = value;
            ApplicableSchemaVersions = applicableSchemaVersions;
        }

        public string[] ApplicableSchemaVersions { get; set; }

        public VariationReason Value { get; set; }
    }
}