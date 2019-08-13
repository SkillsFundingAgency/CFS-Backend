using System.Runtime.Serialization;

namespace CalculateFunding.Models.Calcs
{
    public enum TemplateMappingEntityType
    {
        [EnumMember(Value = "Calculation")]
        Calculation = 0,

        [EnumMember(Value = "ReferenceData")]
        ReferenceData = 1,
    }
}
