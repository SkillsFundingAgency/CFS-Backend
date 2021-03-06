﻿using System.Collections.Generic;
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
        /// Desired configuration type
        /// </summary>
        [Required]
        public ProfileConfigurationType ConfigurationType { get; set; }

        [JsonIgnore] 
        public decimal FundingLineTotalChange => FundingLineTotal - ExistingFundingLineTotal;
    }
}
