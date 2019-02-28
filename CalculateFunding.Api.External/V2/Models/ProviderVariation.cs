using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    public class ProviderVariation
    {
        /// <summary>
        /// Optional reasons for the provider variation. These reasons are in addition to open and close reason.
        /// Possible values:
        /// * AuthorityFieldUpdated
        /// * EstablishmentNumberFieldUpdated
        /// * DfeEstablishmentNumberFieldUpdated
        /// * NameFieldUpdated
        /// * LACodeFieldUpdated
        /// * LegalNameFieldUpdated
        /// </summary>
        public Collection<string> VariationReasons { get; set; }

        /// <summary>
        /// Collection of successor providers
        /// </summary>
        public Collection<ProviderInformationModel> Successors { get; set; }

        /// <summary>
        /// Collection of predecessor providers
        /// </summary>
        public Collection<ProviderInformationModel> Predecessors { get; set; }

        /// <summary>
        /// Optional open reason from the list of GIAS Open Reasons
        /// Possible values:
        /// * Not applicable
        /// * Result of Amalgamation
        /// * Result of Closure
        /// * New Provision
        /// * Fresh Start
        /// * Change in status
        /// * Not Recorded
        /// * New Nursery School
        /// * Change Religious Character
        /// * Former Independent
        /// * Academy Free School
        /// * Academy Converter
        /// * Free Special School
        /// </summary>
        public string OpenReason { get; set; }

        /// <summary>
        /// Optional close reason from list of GIAS Close Reasons
        /// Possible values:
        /// * Not applicable
        /// * Result of Amalgamation/Merger
        /// * Close Nursery School
        /// * Academy Converter
        /// * Fresh Start
        /// * Closure
        /// * For Academy
        /// * Academy Free School
        /// * Change Religious Character
        /// * Does not meet criteria for registration
        /// * Change in status
        /// * De-registered
        /// * Transferred to new sponsor
        /// </summary>
        public string CloseReason { get; set; }
    }
}
