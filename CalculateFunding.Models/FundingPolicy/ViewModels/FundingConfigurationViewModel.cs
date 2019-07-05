using System.Collections.Generic;

namespace CalculateFunding.Models.FundingPolicy.ViewModels
{
    public class FundingConfigurationViewModel
    {
        /// <summary>
        /// Organisational groupings for funding feed publishing
        /// </summary>
        public IEnumerable<OrganisationGroupingConfiguration> OrganisationGroupings { get; set; }
    }
}
