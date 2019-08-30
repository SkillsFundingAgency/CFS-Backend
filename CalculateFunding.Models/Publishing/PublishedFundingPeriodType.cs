﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// The periods of time a period can relate to.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishedFundingPeriodType
    {
        /// <summary>
        /// An academic year (early September till end of July).
        /// </summary>
        AY,

        /// <summary>
        /// A financial year (1 April to 31 March).
        /// </summary>
        FY,

        /// <summary>
        /// Calendar Year
        /// </summary>
        CY,

        /// <summary>
        /// Employer Ownership Fund
        /// </summary>
        EOF,

        /// <summary>
        /// Employer Ownership Pilot
        /// </summary>
        EOP,

        /// <summary>
        /// European Social Fund
        /// </summary>
        ESF,

        /// <summary>
        /// Apprenticeship Levy
        /// </summary>
        LEVY,

        /// <summary>
        /// National Careers Service
        /// </summary>
        NCS,

        /// <summary>
        /// Non-Levy Apprenticeships
        /// </summary>
        NONLEVY,
    }
}