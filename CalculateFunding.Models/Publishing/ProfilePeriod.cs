using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// A funding line profile period (e.g. the 1st March payment in 2019), with relevant value data.
    /// The composite key for the entity is Type, TypeValue, Year and Occurrence
    /// </summary>
    public class ProfilePeriod
    {
        /// <summary>
        /// The type of the period (e.g. CalendarMonth).
        /// </summary>
        [EnumDataType(typeof(ProfilePeriodType))]
        [JsonProperty("type")]
        public ProfilePeriodType Type { get; set; }

        /// <summary>
        /// The value identifier for this period (e.g. if type is 'Calendar Month', this could be 'April').
        /// </summary>
        [JsonProperty("typeValue")]
        public string TypeValue { get; set; }

        /// <summary>
        /// Which year is the period in.
        /// </summary>
        [JsonProperty("year")]
        public int Year { get; set; }

        /// <summary>
        /// Which occurrance this month (note that this is 1 indexed).
        /// Use this to support multiple Funding Line Periods/Profiles in a single Type/TypeValue period
        /// eg April 2020 when three payments are made in this month, the ProfilePeriods array will have three FundingLinePeriods returned in the array with Occurrence set to 1, 2 and 3
        /// </summary>
        [JsonProperty("occurrence")]
        public int Occurrence { get; set; }

        /// <summary>
        /// The amount of the profiled value, in pence
        /// </summary>
        [JsonProperty("profiledValue")]
        public decimal ProfiledValue { get; set; }

        /// <summary>
        /// The funding period code for the funding. eg FY-2020. This will match the distrubution period this profile is paid in.
        /// </summary>
        [JsonProperty("distributionPeriodId")]
        public string DistributionPeriodId { get; set; }
    }
}
