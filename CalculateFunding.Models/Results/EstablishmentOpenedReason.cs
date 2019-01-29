using System.Runtime.Serialization;

namespace CalculateFunding.Models.Results
{
	public enum EstablishmentOpenedReason
	{
		[EnumMember(Value = "Not applicable")]
		NotApplicable,

		[EnumMember(Value = "Result of Amalgamation")]
		ResultOfAmalgamation,

		[EnumMember(Value = "Result of Closure")]
		ResultOfClosure,

		[EnumMember(Value = "New Provision")]
		NewProvision,

		[EnumMember(Value = "Fresh Start")]
		FreshStart,

		[EnumMember(Value = "Change in status")]
		ChangeInStatus,

		[EnumMember(Value = "Not Recorded")]
		NotRecorded,

		[EnumMember(Value = "New Nursery School")]
		NewNurserySchool,

		[EnumMember(Value = "Change Religious Character")]
		ChangeReligiousCharacter,

		[EnumMember(Value = "Former Independent")]
		FormerIndependent,

		[EnumMember(Value = "Academy Free Schools")]
		AcademyFreeSchool,

		[EnumMember(Value = "Academy Converter")]
		AcademyConverter,

		[EnumMember(Value = "Free Special School")]
		FreeSpecialSchool
	}
}
