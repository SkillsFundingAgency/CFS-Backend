using System;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Repositories.Common.Sql;

namespace CalculateFunding.Repositories.Providers
{
    public abstract class ProviderBaseEntity : DbEntity
    {
        [Required]
        public string UKPRN { get; set; }
        [Required]
        public string URN { get; set; }
        public string Name { get; set; }
        public string Authority { get; set; }
        public DateTimeOffset? OpenDate { get; set; }
        public DateTimeOffset? CloseDate { get; set; }
        public string EstablishmentNumber { get; set; }
        public string EstablishmentName { get; set; }
        public string EstablishmentType { get; set; }
        public string EstablishmentTypeGroup { get; set; }
        public string EstablishmentStatus { get; set; }
        public string ReasonEstablishmentOpened { get; set; }
        public string ReasonEstablishmentClosed { get; set; }
        public string PhaseOfEducation { get; set; }
        public int? StatutoryLowAge { get; set; }
        public int? StatutoryHighAge { get; set; }
        public string Boarders { get; set; }
        public string NurseryProvision { get; set; }
        public string OfficialSixthForm { get; set; }
        public string Gender { get; set; }
        public string ReligiousCharacter { get; set; }
        public string ReligiousEthos { get; set; }
        public string Diocese { get; set; }
        public string AdmissionsPolicy { get; set; }
        public int? SchoolCapacity { get; set; }
        public string SpecialClasses { get; set; }
        public DateTimeOffset? CensusDate { get; set; }
        public int? NumberOfPupils { get; set; }
        public int? NumberOfBoys { get; set; }
        public int? NumberOfGirls { get; set; }
        public decimal? PercentageFSM { get; set; }
        public string TrustSchoolFlag { get; set; }
        public string Trusts { get; set; }
        public string SchoolSponsorFlag { get; set; }
        public string SchoolSponsors { get; set; }
        public string FederationFlag { get; set; }
        public string Federations { get; set; }
        public string FEHEIdentifier { get; set; }
        public string FurtherEducationType { get; set; }
        public DateTimeOffset? OfstedLastInspectionDate { get; set; }
        public string OfstedSpecialMeasures { get; set; }
        public string OfstedRating { get; set; }
        public DateTimeOffset? LastChangedDate { get; set; }
        public string Street { get; set; }
        public string Locality { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
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
        public string TypeOfResourcedProvision { get; set; }
        public int? ResourcedProvisionOnRoll { get; set; }
        public int? ResourcedProvisionCapacity { get; set; }
        public int? SenUnitOnRoll { get; set; }
        public int? SenUnitCapacity { get; set; }
        public string GOR { get; set; }
        public string DistrictAdministrative { get; set; }
        public string AdministrativeWard { get; set; }
        public string ParliamentaryConstituency { get; set; }
        public string UrbanRural { get; set; }
        public string GSSLACode { get; set; }
        public string CensusAreaStatisticWard { get; set; }
        public string MSOA { get; set; }
        public string LSOA { get; set; }
        public int? SENStat { get; set; }
        public int? SENNoStat { get; set; }
        public string RSCRegion { get; set; }
    }
}