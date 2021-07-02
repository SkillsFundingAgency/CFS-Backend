using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingLineProfile
    {
        /// <summary>
        /// Funding line code
        /// </summary>
        public string FundingLineCode { get; set; }

        /// <summary>
        /// Funding line display name
        /// </summary>
        public string FundingLineName { get; set; }

        /// <summary>
        /// Total funding line amount - from calculations
        /// </summary>
        public decimal? FundingLineAmount { get; set; }

        /// <summary>
        /// Total funding allocated to profile patterns - may be different to the funding line amount when custom profiles are set
        /// </summary>
        public decimal? ProfilePatternTotal { get; set; }

        /// <summary>
        /// Total amount already paid during this period
        /// </summary>
        public decimal AmountAlreadyPaid { get; set; }

        /// <summary>
        /// The amount which is unpaid in this period
        /// </summary>
        public decimal? RemainingAmount { get; set; }

        /// <summary>
        /// Carry over amount to next funding period
        /// </summary>
        public decimal? CarryOverAmount { get; set; }

        /// <summary>
        /// The total profile values with carry over included. May be different to Funding Line Total
        /// </summary>
        public decimal? ProfilePatternTotalWithCarryOver { get; set; }

        /// <summary>
        /// Provider ID
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        /// Provider name
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// UKPRN of the provider
        /// </summary>
        public string UKPRN { get; set; }

        /// <summary>
        /// Profiling service pattern key
        /// </summary>
        public string ProfilePatternKey { get; set; }

        /// <summary>
        /// Profile pattern display name
        /// </summary>
        public string ProfilePatternName { get; set; }

        /// <summary>
        /// Profile pattern description
        /// </summary>
        public string ProfilePatternDescription { get; set; }

        /// <summary>
        /// Is this provider configured with a custom profile
        /// </summary>
        public bool IsCustomProfile { get; set; }

        public Reference LastUpdatedUser { get; set; }

        public DateTimeOffset? LastUpdatedDate { get; set; }

        /// <summary>
        /// Profile totals
        /// </summary>
        public IEnumerable<ProfileTotal> ProfileTotals { get; set; }

        /// <summary>
        /// Funding line errors
        /// </summary>
        public IEnumerable<PublishedProviderError> Errors { get; set; }
    }
}
