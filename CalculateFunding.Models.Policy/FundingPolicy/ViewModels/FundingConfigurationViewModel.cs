using CalculateFunding.Models.Providers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.FundingPolicy.ViewModels
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
        
        /// <summary>
        /// The mode this funding configuration will operate under during approve and refresh
        /// </summary>
        public ApprovalMode ApprovalMode { get; set; }

        /// <summary>
        /// Provider Source
        /// </summary>
        public ProviderSource ProviderSource { get; set; }

        /// <summary>
        /// Payment Organisation Source
        /// </summary>
        public PaymentOrganisationSource PaymentOrganisationSource { get; set; }
    }
}
