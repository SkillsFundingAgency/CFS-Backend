using System;
using Newtonsoft.Json;

namespace CalculateFunding.Api.External.V2.Models
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
        /// The name of this provider as recorded in Get Information about Schools attribute 'Name'
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The providers legal entity name as recorded on UK Register of Learning Providers and OR Companies House (NO CURRENT SOURCE)
        /// </summary>
        public string LegalName { get; set; }

        /// <summary>
        /// The UK provider reference number as recorded from the providers registration on the UK Register of Learning Providers. Get Information About Schools attribute name 'UKPRN'
        /// </summary>
        public string UkPrn { get; set; }

        /// <summary>
        /// The Unique Provider Identification Number, the main provider reference number in the Provider Infomation Management System (PIMS) (NO CURRENT SOURCE)
        /// </summary>
        public string Upin { get; set; }

        /// <summary>
        /// The unique reference number, the provider identifier from Get Information about schools attribute 'URN'
        /// </summary>
        public string Urn { get; set; }

        /// <summary>
        /// The four digit establishment number for a school registered by the Department of Education. Get Information About Schools attribute 'EstablishmentNumber'
        /// </summary>
        public string DfeEstablishmentNumber { get; set; }

        /// <summary>
        /// The Local Authority Establishment Number, as a combination of the providers establishment number and Local authority code. Concatenated from Get Information About Schools attributes LA (code) and EstablishmentNumber
        /// </summary>
        public string EstablishmentNumber { get; set; }

        /// <summary>
        /// The three digit code from the Local Authrority  pre 2011 Office of National Statistics coding system. Get Information About Schools attribute LA (code)
        /// </summary>
        public string LaCode { get; set; }

        /// <summary>
        /// The name of the local authority that the provider is located from Get Information About Schools attribute LA (name)
        /// </summary>
        public string LocalAuthority { get; set; }

        /// <summary>
        /// The top level description of the type of provider.  Get Information about Schools attribute 'EstablishmentTypeGroup (name)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The low level description of the providers type. Get Information about schools attribute 'TypeOfEstablishment (name)'
        /// </summary>
        public string SubType { get; set; }

        /// <summary>
        /// The date that this provider entity was opened as recorded in Get Information about Schools attribute 'OpenDate'
        /// </summary>
        public DateTimeOffset? OpenDate { get; set; }

        /// <summary>
        /// The date this provider ceased to be a provider of education or skills. Get Information About Schools attribute CloseDate
        /// </summary>
        public DateTimeOffset? CloseDate { get; set; }

        /// <summary>
        /// The Account number of the provider on the Department Of Educations CRM (NO CURRENT SOURCE)
        /// </summary>
        public string CrmAccountId { get; set; }

        /// <summary>
        /// The account number of the provider on the Department Of Educations finance system (NO CURRENT SOURCE)
        /// </summary>
        public string NavVendorNo { get; set; }

        /// <summary>
        /// The current status of provider as a deliverer of education and skills from Get Information about schools attribute EstablishmentStatus (name)
        /// </summary>
        public string Status { get; set; }
    }
}
