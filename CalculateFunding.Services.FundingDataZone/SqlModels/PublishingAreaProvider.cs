using Dapper.Contrib.Extensions;
using System;

namespace CalculateFunding.Services.FundingDataZone.SqlModels
{
    [Table("Provider")]
    public class PublishingAreaProvider
    {
        public int Id { get; set; }

        public string ProviderSnapshotId { get; set; }

        public string ProviderId { get; set; }

        public string Name { get; set; }

        public string URN { get; set; }

        public string UKPRN { get; set; }

        public string UPIN { get; set; }

        public string EstablishmentNumber { get; set; }

        public string DfeEstablishmentNumber { get; set; }

        public string Authority { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        public DateTimeOffset? DateOpened { get; set; }

        public DateTimeOffset? DateClosed { get; set; }

        public string LACode { get; set; }

        public string NavVendorNo { get; set; }

        public string CrmAccountId { get; set; }

        public string LegalName { get; set; }

        public string Status { get; set; }

        public string PhaseOfEducation { get; set; }

        public string ReasonEstablishmentOpened { get; set; }

        public string ReasonEstablishmentClosed { get; set; }

        public string Successor { get; set; }

        public string Town { get; set; }

        public string Postcode { get; set; }

        public string TrustName { get; set; }

        public string TrustCode { get; set; }

        public string LocalAuthorityName { get; set; }

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

        public string CountryCode { get; set; }

        public string CountryName { get; set; }

        public string LocalGovernmentGroupTypeCode { get; set; }

        public string LocalGovernmentGroupTypeName { get; set; }

        public string TrustStatus { get; set; }

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

        public int? ProviderStatusId { get; set; }

        [Computed]
        public string Predecessors { get; set; }

        [Computed]
        public string Successors { get; set; }

        [Computed]
        public bool Indicative { get; set; }
    }
}
