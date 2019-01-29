using CalculateFunding.Models.Converters;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Converters;


namespace CalculateFunding.Models.Results
{
    public class MasterProviderModel
    {
        public string MasterCRMAccountId { get; set; }

        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset? MasterDateClosed { get; set; }

        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset? MasterDateOpened { get; set; }

        public string MasterDfEEstabNo { get; set; }

        public string MasterDfELAEstabNo { get; set; }

        public string MasterLocalAuthorityCode { get; set; }

        public string MasterLocalAuthorityName { get; set; }

        public string MasterProviderLegalName { get; set; }

        public string MasterProviderName { get; set; }

        public string MasterProviderStatusName { get; set; }

        public string MasterProviderTypeGroupName { get; set; }

        public string MasterProviderTypeName { get; set; }

        public string MasterUKPRN { get; set; }

        public string MasterUPIN { get; set; }

        public string MasterURN { get; set; }

        public string MasterPhaseOfEducation { get; set; }

        public string MasterNavendorNo { get; set; }

	    [JsonConverter(typeof(StringEnumConverter))]
		public EstablishmentOpenedReason? MasterReasonEstablishmentOpened { get; set; }

	    [JsonConverter(typeof(StringEnumConverter))]
		public EstablishmentClosedReason? MasterReasonEstablishmentClosed { get; set; }

	    public string MasterSuccessor { get; set; }
    }
}
