using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CalculateFunding.Services.Profiling.Models
{
    public class ReProfileRequest
    {
        /// <summary>
        /// Funding Stream ID
        /// </summary>
        [Required]
        public string FundingStreamId { get; set; }

        /// <summary>
        /// Funding Stream Period
        /// </summary>
        [Required]
        public string FundingPeriodId { get; set; }

        /// <summary>
        /// Funding Line Code
        /// </summary>
        [Required]
        public string FundingLineCode { get; set; }

        /// <summary>
        /// Profile pattern key - null or empty string for default pattern or a valid profile pattern key for this funding stream/period/line.
        /// </summary>
        public string ProfilePatternKey { get; set; }

        /// <summary>
        /// The existing funding line total
        /// </summary>
        [Required]
        public decimal ExistingFundingLineTotal { get; set; }

        /// <summary>
        /// The current / target funding line total
        /// </summary>
        [Required]
        public decimal FundingLineTotal { get; set; }

        /// <summary>
        /// Profile periods which have already been paid
        /// </summary>
        public IEnumerable<ExistingProfilePeriod> ExistingPeriods { get; set; }

        /// <summary>
        /// All exeisting profile periods
        /// </summary>
        public IEnumerable<ExistingProfilePeriod> AllExistingPeriods { get; set; }

        /// <summary>
        /// Flag indicating whether the re profiling
        /// should use a new opener or new opener catchup if blank then use new allocation strategy
        /// </summary>
        public MidYearType? MidYearType { get; set; }

        /// <summary>
        /// The index into the ordered refresh profile periods
        /// to start paying from
        /// </summary>
        public int? VariationPointerIndex { get; set; }

        /// <summary>
        /// A flag used to determine if we are requesting a re-profile which has already happened
        /// </summary>
        public bool AlreadyPaidUpToIndex { get; set; }

        [JsonIgnore] 
        public decimal FundingLineTotalChange => FundingLineTotal - ExistingFundingLineTotal;
    }
}
