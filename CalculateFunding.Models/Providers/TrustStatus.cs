using System.Runtime.Serialization;

namespace CalculateFunding.Models.Providers
{
    public enum TrustStatus
    {
        [EnumMember(Value = "Not applicable")]
        NotApplicable,

        [EnumMember(Value = "Not supported by a trust")]
        NotSupportedByATrust,

        [EnumMember(Value = "Supported by a trust")]
        SupportedByATrust,

        [EnumMember(Value = "Supported by a single-academy trust")]
        SupportedByASingleAacademyTrust,

        [EnumMember(Value = "Supported by a multi-academy trust")]
        SupportedByAMultiAcademyTrust,
    }
}
