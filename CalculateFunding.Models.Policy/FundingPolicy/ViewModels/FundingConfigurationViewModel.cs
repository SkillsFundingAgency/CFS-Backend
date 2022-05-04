using CalculateFunding.Models.Providers;
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
        /// Variations to run during release management
        /// </summary>
        public IEnumerable<VariationType> ReleaseManagementVariations { get; set; }

        /// <summary>
        /// Error detectors
        /// </summary>
        public IEnumerable<string> ErrorDetectors { get; set; }

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

        /// <summary>
        /// Update Core Provider Version to track latest provider version
        /// </summary>
        public UpdateCoreProviderVersion UpdateCoreProviderVersion { get; set; }

        /// <summary>
        /// Indicates whether custom profiling changes are enabled for the fundingstream or not
        /// </summary>
        public bool EnableUserEditableCustomProfiles { get; set; }

        /// <summary>
        /// Indicates whether rule based profiling changes are enabled for the fundingstream or not
        /// </summary>
        public bool EnableUserEditableRuleBasedProfiles { get; set; }

        public bool EnableConverterDataMerge { get; set; }

        public bool EnableInformationLineAggregation { get; set; }

        public bool SuccessorCheck { get; set; }

        /// <summary>
        /// This property is used on PSG so that we don't populate the predecessor on the 
        /// current version from the provider on creation so that the property can be
        /// populated during the ClosureWithSuccessor variation strategy
        /// </summary>
        public bool DisablePopulatePredecessorOnCreate { get; set; }

        public IEnumerable<string> IndicativeOpenerProviderStatus { get; set; }

        public bool RunCalculationEngineAfterCoreProviderUpdate { get; set; }

        /// <summary>
        /// Funding stream IDs which this funding stream can reference for published funding
        /// </summary>
        public IEnumerable<string> AllowedPublishedFundingStreamsIdsToReference { get; set; }

        public IEnumerable<FundingConfigurationChannel> ReleaseChannels { get; set; }

        public IEnumerable<ReleaseActionGroup> ReleaseActionGroups { get; set; }

        public bool EnableCarryForward { get; set; }

        /// <summary>
        /// Indicates which channel should be used for spec to spec
        /// </summary>
        public string SpecToSpecChannelCode { get; set; }
    }
}
