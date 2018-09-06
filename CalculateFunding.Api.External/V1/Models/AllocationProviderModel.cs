using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V1.Models
{
	/// <summary>
	/// Fields are made available only if it is provided from the provider datasource
	/// </summary>
	[Serializable]
    public class AllocationProviderModel
    {
        [JsonIgnore]
        public string ProviderId { get; set; }

        /// <summary>
        /// The provider name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The provider legal name
        /// </summary>
        public string LegalName { get; set; }

        /// <summary>
        /// The provider ukprn
        /// </summary>
        public string UkPrn { get; set; }

        /// <summary>
        /// The provider upin
        /// </summary>
        public string Upin { get; set; }

        /// <summary>
        /// The provider urn
        /// </summary>
        public string Urn { get; set; }

        /// <summary>
        /// The provider dfe establishment number
        /// </summary>
        public string DfeEstablishmentNumber { get; set; }

        /// <summary>
        /// The provider establishment number
        /// </summary>
        public string EstablishmentNumber { get; set; }

        /// <summary>
        /// The provider la code
        /// </summary>
        public string LaCode { get; set; }

        /// <summary>
        /// The provider authority
        /// </summary>
        public string LocalAuthority { get; set; }

        /// <summary>
        /// The provider type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The provider sub type
        /// </summary>
        public string SubType { get; set; }

        /// <summary>
        /// The provider open date
        /// </summary>
        public DateTimeOffset? OpenDate { get; set; }

        /// <summary>
        /// The provider closed date
        /// </summary>
        public DateTimeOffset? CloseDate { get; set; }

        /// <summary>
        /// The provider crm account id
        /// </summary>
        public string CrmAccountId { get; set; }

        /// <summary>
        /// The provider nav vendor number
        /// </summary>
        public string NavVendorNo { get; set; }

        /// <summary>
        /// The provider status
        /// </summary>
        public string Status { get; set; }
    }
}
