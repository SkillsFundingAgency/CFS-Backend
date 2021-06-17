using System;
using System.ComponentModel.DataAnnotations;
using Dapper.Contrib.Extensions;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("Provider")]
    public class PublishingAreaProvider
    {
        public int Id { get; set; }

        [Required]
        public int ProviderSnapshotId { get; set; }

        public string ProviderId { get; set; }

        [Required]
        public string Name { get; set; }

        public string URN { get; set; }

        [Required]
        public string UKPRN { get; set; }

        public string UPIN { get; set; }

        [Display(Name = "Establishment Number")]
        public string EstablishmentNumber { get; set; }

        [Display(Name = "DfE Establishment Number")]
        public string DfeEstablishmentNumber { get; set; }

        [Display(Name = "Local Authority Name")]
        public string Authority { get; set; }

        [Display(Name = "Provider type")]
        public string ProviderType { get; set; }

        [Display(Name = "Provider subtype")]
        public string ProviderSubType { get; set; }

        [Display(Name = "Date opened")]
        public DateTimeOffset? DateOpened { get; set; }

        [Display(Name = "Date closed")]
        public DateTimeOffset? DateClosed { get; set; }

        [Display(Name = "Local authorirty code (LACode)")]
        [Required]
        public string LACode { get; set; }

        [Display(Name = "NAV vendor number")]
        public string NavVendorNo { get; set; }

        [Display(Name = "CRM account ID")]
        public string CrmAccountId { get; set; }

        [Display(Name = "Legal name")]
        public string LegalName { get; set; }

        [Display(Name = "Phase of education")]
        public string PhaseOfEducation { get; set; }

        [Display(Name = "Reason for opening")]
        public string ReasonEstablishmentOpened { get; set; }

        [Display(Name = "Reason for closing")]
        public string ReasonEstablishmentClosed { get; set; }

        public string Town { get; set; }

        public string Postcode { get; set; }

        [Display(Name = "Trust name")]
        public string TrustName { get; set; }

        [Display(Name = "Trust code")]
        public string TrustCode { get; set; }

        [Display(Name = "Companies house number")]
        public string CompaniesHouseNumber { get; set; }

        [Display(Name = "Group ID number")]
        public string GroupIdNumber { get; set; }

        [Display(Name = "RSC region name")]
        public string RscRegionName { get; set; }

        [Display(Name = "RSC region code")]
        public string RscRegionCode { get; set; }

        [Display(Name = "Government office region name")]
        public string GovernmentOfficeRegionName { get; set; }

        [Display(Name = "Government office region code")]
        public string GovernmentOfficeRegionCode { get; set; }

        [Display(Name = "District name")]
        public string DistrictName { get; set; }

        [Display(Name = "District code")]
        public string DistrictCode { get; set; }

        [Display(Name = "Ward name")]
        public string WardName { get; set; }

        [Display(Name = "Ward code")]
        public string WardCode { get; set; }

        [Display(Name = "Census ward name")]
        public string CensusWardName { get; set; }

        [Display(Name = "Census ward code")]
        public string CensusWardCode { get; set; }

        [Display(Name = "Middle super output area name")]
        public string MiddleSuperOutputAreaName { get; set; }

        [Display(Name = "Middle super output area code")]
        public string MiddleSuperOutputAreaCode { get; set; }

        [Display(Name = "Lower super output area name")]
        public string LowerSuperOutputAreaName { get; set; }

        [Display(Name = "Lower super output area code")]
        public string LowerSuperOutputAreaCode { get; set; }

        [Display(Name = "Parliamentary constituency name")]
        public string ParliamentaryConstituencyName { get; set; }

        [Display(Name = "Parliamentary constituency code")]
        public string ParliamentaryConstituencyCode { get; set; }

        [Display(Name = "Country code")]
        public string CountryCode { get; set; }

        [Display(Name = "Country name")]
        public string CountryName { get; set; }

        [Display(Name = "Local government group type code")]
        public string LocalGovernmentGroupTypeCode { get; set; }

        [Display(Name = "Local government group type name")]
        public string LocalGovernmentGroupTypeName { get; set; }

        [Display(Name = "Trust status")]
        public string TrustStatus { get; set; }

        [Display(Name = "Parent organisation")]
        public int? PaymentOrganisationId { get; set; }

        [Computed]
        public string PaymentOrganisationName { get; set; }

        [Computed]
        public string PaymentOrganisationUpin { get; set; }

        [Computed]
        public string PaymentOrganisationTrustCode { get; set; }

        [Computed]
        public string PaymentOrganisationLaCode { get; set; }

        [Computed]
        public string PaymentOrganisationUrn { get; set; }

        [Computed]
        public string PaymentOrganisationCompanyHouseNumber { get; set; }

        [Computed]
        public string PaymentOrganisationOrganisationType { get; set; }

        [Computed]
        public string PaymentOrganisationUkprn { get; set; }

        [Display(Name = "Provider type code")]
        public string ProviderTypeCode { get; set; }

        [Display(Name = "Provider subtype code")]
        public string ProviderSubTypeCode { get; set; }

        [Display(Name = "Status code")]
        public string StatusCode { get; set; }

        [Display(Name = "Reason establishment opened code")]
        public string ReasonEstablishmentOpenedCode { get; set; }

        [Display(Name = "Reason establishment closed code")]
        public string ReasonEstablishmentClosedCode { get; set; }

        [Display(Name = "Phase of education code")]
        public string PhaseOfEducationCode { get; set; }

        [Display(Name = "Statutory low age")]
        public string StatutoryLowAge { get; set; }

        [Display(Name = "Statutory high age")]
        public string StatutoryHighAge { get; set; }

        [Display(Name = "Official sixth form code")]
        public string OfficialSixthFormCode { get; set; }

        [Display(Name = "Official sixth form name")]
        public string OfficialSixthFormName { get; set; }

        [Display(Name = "Previous local authority code")]
        public string PreviousLaCode { get; set; }

        [Display(Name = "Previous local authority name")]
        public string PreviousLaName { get; set; }

        [Display(Name = "Previous establishment number")]
        public string PreviousEstablishmentNumber { get; set; }

        [Display(Name = "Provider status")]
        [Required]
        public int? ProviderStatusId { get; set; }

        [Display(Name = "Provider status")]
        [Computed]
        public string ProviderStatusName { get; set; }

        [Computed]
        public string Predecessors { get; set; }

        [Computed]
        public string Successors { get; set; }
    }
}
