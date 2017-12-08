using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;

namespace CalculateFunding.Models.Providers
{
    [SearchIndex()]
    public class ProviderIndex
    {
        [Key]
        [IsSearchable]
        public string Urn { get; set; }

        [IsSearchable]
        public string UKPRN { get; set; }

        public string Name { get; set; }
        [IsSearchable]
        [IsFacetable]
        public string Authority { get; set; }
        public DateTimeOffset? OpenDate { get; set; }
        public DateTimeOffset? CloseDate { get; set; }
        [IsSearchable]
        public string EstablishmentNumber { get; set; }
        public string EstablishmentName { get; set; }
        [IsFacetable]
        public string EstablishmentType { get; set; }
        [IsFacetable]
        public string EstablishmentTypeGroup { get; set; }
        [IsFacetable]
        public string EstablishmentStatus { get; set; }
        public string ReasonEstablishmentOpened { get; set; }
        public string ReasonEstablishmentClosed { get; set; }
        [IsFacetable]
        public string PhaseOfEducation { get; set; }
        public int? StatutoryLowAge { get; set; }
        public int? StatutoryHighAge { get; set; }
        public string Boarders { get; set; }
        [IsFacetable]
        public string NurseryProvision { get; set; }
        [IsFacetable]
        public string OfficialSixthForm { get; set; }
        [IsFacetable]
        public string Gender { get; set; }
        [IsFacetable]
        public string ReligiousCharacter { get; set; }
        public string ReligiousEthos { get; set; }
        public string Diocese { get; set; }
        [IsFacetable]
        public string AdmissionsPolicy { get; set; }
        public int? SchoolCapacity { get; set; }
        public string SpecialClasses { get; set; }
        public DateTimeOffset? CensusDate { get; set; }
        public int? NumberOfPupils { get; set; }
        public int? NumberOfBoys { get; set; }
        public int? NumberOfGirls { get; set; }
        public decimal? PercentageFSM { get; set; }
        [IsFacetable]
        public string TrustSchoolFlag { get; set; }
        [IsFacetable]
        public string Trusts { get; set; }
        [IsFacetable]
        public string SchoolSponsorFlag { get; set; }
        [IsFacetable]
        public string SchoolSponsors { get; set; }
        [IsFacetable]
        public string FederationFlag { get; set; }
        [IsFacetable]
        public string Federations { get; set; }
        [IsSearchable]
        public string FEHEIdentifier { get; set; }
        [IsFacetable]
        public string FurtherEducationType { get; set; }
        public DateTimeOffset? OfstedLastInspectionDate { get; set; }
        [IsFacetable]
        public string OfstedSpecialMeasures { get; set; }
        [IsFacetable]
        public string OfstedRating { get; set; }
        public DateTimeOffset? LastChangedDate { get; set; }
        [IsSearchable]
        public string Street { get; set; }
        [IsSearchable]
        public string Locality { get; set; }
        public string Address3 { get; set; }
        [IsSearchable]
        public string Town { get; set; }
        [IsSearchable]
        public string County { get; set; }
        [IsSearchable]
        public string Postcode { get; set; }
        public string Country { get; set; }
        public int? Easting { get; set; }
        public int? Northing { get; set; }
        public string Website { get; set; }
        public string Telephone { get; set; }
        public string TeenMoth { get; set; }
        public int? TeenMothPlaces { get; set; }
        public string CCF { get; set; }
        public string SENPRU { get; set; }
        public string EBD { get; set; }
        public int? PRUPlaces { get; set; }
        public string FTProv { get; set; }
        public string EdByOther { get; set; }
        public string Section41Approved { get; set; }
        public string SEN1 { get; set; }
        [IsFacetable]
        public string TypeOfResourcedProvision { get; set; }
        public int? ResourcedProvisionOnRoll { get; set; }
        public int? ResourcedProvisionCapacity { get; set; }
        public int? SenUnitOnRoll { get; set; }
        public int? SenUnitCapacity { get; set; }
        public string GOR { get; set; }
        [IsFacetable]
        public string DistrictAdministrative { get; set; }
        [IsFacetable]
        public string AdministrativeWard { get; set; }
        [IsFacetable]
        public string ParliamentaryConstituency { get; set; }
        [IsFacetable]
        public string UrbanRural { get; set; }
        [IsSearchable]
        public string GSSLACode { get; set; }
        public string CensusAreaStatisticWard { get; set; }
        public string MSOA { get; set; }
        public string LSOA { get; set; }
        public int? SENStat { get; set; }
        public int? SENNoStat { get; set; }
        [IsFacetable]
        public string RSCRegion { get; set; }

    }
}