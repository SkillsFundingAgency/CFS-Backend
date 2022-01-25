using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.FundingDataZone
{
    public class Provider
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string ProviderSnapshotId { get; set; }

        [Required]
        public string ProviderId { get; set; }

        [Required]
        public string Name { get; set; }

        public string URN { get; set; }

        public string UKPRN { get; set; }

        public string UPIN { get; set; }

        public string EstablishmentNumber { get; set; }

        public string DfeEstablishmentNumber { get; set; }

        public string Authority { get; set; }

        [Required]
        public string ProviderType { get; set; }

        [Required]
        public string ProviderSubType { get; set; }

        public DateTimeOffset? DateOpened { get; set; }

        public DateTimeOffset? DateClosed { get; set; }

        public string LACode { get; set; }

        public string NavVendorNo { get; set; }

        public string CrmAccountId { get; set; }

        public string LegalName { get; set; }

        [Required]
        public string Status { get; set; }

        public string PhaseOfEducation { get; set; }

        public string ReasonEstablishmentOpened { get; set; }

        public string ReasonEstablishmentClosed { get; set; }

        public string Town { get; set; }

        public string Postcode { get; set; }

        public string TrustName { get; set; }

        public string TrustCode { get; set; }

        public string CompaniesHouseNumber { get; set; }

        public string GroupIdNumber { get; set; }

        public string RscRegionName { get; set; }

        public string RscRegionCode { get; set; }

        public string GovernmentOfficeRegionName { get; set; }

        public string GovernmentOfficeRegionCode { get; set; }

        public string DistrictName { get; set; }

        public string DistrictCode { get; set; }

        public string WardName { get; set; }

        public string WardCode { get; set; }

        public string CensusWardName { get; set; }

        public string CensusWardCode { get; set; }

        public string MiddleSuperOutputAreaName { get; set; }

        public string MiddleSuperOutputAreaCode { get; set; }

        public string LowerSuperOutputAreaName { get; set; }

        public string LowerSuperOutputAreaCode { get; set; }

        public string ParliamentaryConstituencyName { get; set; }

        public string ParliamentaryConstituencyCode { get; set; }

        public string LondonRegionCode { get; set; }

        public string LondonRegionName { get; set; }

        public string CountryCode { get; set; }

        public string CountryName { get; set; }

        public string LocalGovernmentGroupTypeCode { get; set; }

        public string LocalGovernmentGroupTypeName { get; set; }

        [Required]
        public string TrustStatus { get; set; }

        [Required]
        public int PaymentOrganisationId { get; set; }

        [Required]
        public string PaymentOrganisationName { get; set; }

        public string PaymentOrganisationUpin { get; set; }

        public string PaymentOrganisationTrustCode { get; set; }

        public string PaymentOrganisationLaCode { get; set; }

        public string PaymentOrganisationUrn { get; set; }

        public string PaymentOrganisationCompanyHouseNumber { get; set; }

        public string PaymentOrganisationType { get; set; }

        public string PaymentOrganisationUkprn { get; set; }

        public string ProviderTypeCode { get; set; }

        public string ProviderSubTypeCode { get; set; }

        public string StatusCode { get; set; }

        public string ReasonEstablishmentOpenedCode { get; set; }

        public string ReasonEstablishmentClosedCode { get; set; }

        public string PhaseOfEducationCode { get; set; }

        public string StatutoryLowAge { get; set; }

        public string StatutoryHighAge { get; set; }

        public string OfficialSixthFormCode { get; set; }

        public string OfficialSixthFormName { get; set; }

        public string PreviousLaCode { get; set; }

        public string PreviousLaName { get; set; }

        public string PreviousEstablishmentNumber { get; set; }

        public string FurtherEducationTypeCode { get; set; }

        public string FurtherEducationTypeName { get; set; }

        public IEnumerable<string> Predecessors { get; set; }

        public IEnumerable<string> Successors { get; set; }
    }
}
