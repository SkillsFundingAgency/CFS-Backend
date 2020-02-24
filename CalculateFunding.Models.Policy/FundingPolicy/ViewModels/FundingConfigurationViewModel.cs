using System.Collections.Generic;

namespace CalculateFunding.Models.FundingPolicy.ViewModels
{
    public class FundingConfigurationViewModel
    {
        public string DefaultTemplateVersion { get; set; }
        
        /// <summary>
        /// Organisational groupings for funding feed publishing
        /// </summary>
        public IEnumerable<OrganisationGroupingConfiguration> OrganisationGroupings { get; set; }

        /// <summary>
        /// Variation strategies
        /// </summary>
        public IEnumerable<VariationType> Variations { get; set; }
    }
}
