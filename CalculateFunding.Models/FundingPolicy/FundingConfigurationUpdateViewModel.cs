using System.Collections.Generic;

namespace CalculateFunding.Models.FundingPolicy
{
    public class FundingConfigurationUpdateViewModel
    {
        /// <summary>
        /// Organisational groupings for funding feed publishing
        /// </summary>
        public IEnumerable<OrganisationGroupingConfiguration> OrganisationGroupings { get; set; }
    }
}
