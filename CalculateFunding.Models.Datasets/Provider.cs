using System;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class Provider : Reference
    {
        [Key]
        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("ukprn")]
        public string UKPRN { get; set; }

        [JsonProperty("authority")]
        public Reference Authority { get; set; }

        [JsonProperty("openedDate")]
        public DateTimeOffset? OpenDate { get; set; }
        [JsonProperty("closedDate")]
        public DateTimeOffset? CloseDate { get; set; }

        public string EstablishmentNumber { get; set; }
        public string EstablishmentName { get; set; }
        public Reference EstablishmentType { get; set; }
        public Reference EstablishmentTypeGroup { get; set; }
        public Reference EstablishmentStatus { get; set; }
        public Reference ReasonEstablishmentOpened { get; set; }
        public Reference ReasonEstablishmentClosed { get; set; }
        public Reference PhaseOfEducation { get; set; }
        public int? StatutoryLowAge { get; set; }
        public int? StatutoryHighAge { get; set; }
        public Reference Boarders { get; set; }
        public Reference NurseryProvision { get; set; }
        public Reference OfficialSixthForm { get; set; }
        public Reference Gender { get; set; }
        public Reference ReligiousCharacter { get; set; }
        public Reference ReligiousEthos { get; set; }
        public Reference Diocese { get; set; }
        public Reference AdmissionsPolicy { get; set; }
        public int? SchoolCapacity { get; set; }
        public Reference SpecialClasses { get; set; }
        public DateTimeOffset? CensusDate { get; set; }
        public int? NumberOfPupils { get; set; }
        public int? NumberOfBoys { get; set; }
        public int? NumberOfGirls { get; set; }

        public decimal? PercentageFSM { get; set; }
        public Reference TrustSchoolFlag { get; set; }
        public Reference Trusts { get; set; }
        public string SchoolSponsorFlag { get; set; }
        public string SchoolSponsors { get; set; }
        public string FederationFlag { get; set; }
        public Reference Federations { get; set; }
        public string FEHEIdentifier { get; set; }
        public string FurtherEducationType { get; set; }
        public DateTimeOffset? OfstedLastInspectionDate { get; set; }
        public Reference OfstedSpecialMeasures { get; set; }
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
        public Reference GOR { get; set; }
        public Reference DistrictAdministrative { get; set; }
        public Reference AdministrativeWard { get; set; }
        public Reference ParliamentaryConstituency { get; set; }
        public Reference UrbanRural { get; set; }
        public string GSSLACode { get; set; }
        public string CensusAreaStatisticWard { get; set; }
        public string MSOA { get; set; }
        public string LSOA { get; set; }
        public int? SENStat { get; set; }
        public int? SENNoStat { get; set; }
        public string RSCRegion { get; set; }
    }

}