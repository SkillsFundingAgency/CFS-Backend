using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.FundingPolicy
{
    public class OrganisationGroupingConfiguration
    {
        /// <summary>
        /// Grouping organisation 
        /// </summary>
        [EnumDataType(typeof(OrganisationIdentifierType))]
        public OrganisationIdentifierType IdentifierType { get; set; }

        [EnumDataType(typeof(GroupingReason))]
        public GroupingReason GroupingReason { get; set; }

        [EnumDataType(typeof(OrganisationGroupingType))]
        public OrganisationGroupingType OrganisationGroupingType { get; set; }

        public IEnumerable<ProviderTypeMatch> ProviderTypeMatch { get; set; }
    }
}