using System.Runtime.Serialization;

namespace CalculateFunding.Models.Specs
{
    public enum TemplateMappingEntityType
    {
        [EnumMember(Value = "Calculation")]
        Calculation = 0,

        [EnumMember(Value = "ReferenceData")]
        ReferenceData = 1,

        [EnumMember(Value = "FundingLine")]
        FundingLine = 2
    }
}
