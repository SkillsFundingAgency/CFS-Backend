using System;

namespace CalculateFunding.Api.External.V2.Models
{
    /// <summary>
    /// Represents an allocation line
    /// </summary>
    [Serializable]
    public class AllocationLine
    {
        /// <summary>
        /// The identifier for the allocation line
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the allocation line
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The short name of the allocation line
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// The funding route of the allocation line
        /// </summary>
        public string FundingRoute { get; set; }

        /// <summary>
        /// Check if contract is required for allocation line
        /// </summary>
        public string ContractRequired { get; set; }
    }
}