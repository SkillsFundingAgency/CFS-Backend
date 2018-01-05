using System;
using CalculateFunding.Models.Datasets;
using CsvHelper;
using CsvHelper.Configuration;

namespace CalculateFunding.Repositories.Providers
{

    public sealed class EdubaseRecordMap : ClassMap<Provider>
    {
        public EdubaseRecordMap()
        {
            
            AutoMap();
            Map(m => m.UKPRN).Name("URN");
            Map(m => m.UKPRN).Name("URN");
            Map(m => m.Name).Name("EstablishmentName");
            Map(m => m.Authority.Id).Name("LA (code)");
            Map(m => m.Authority.Name).Name("LA (name)");
            Map(m => m.EstablishmentType.Id).Name("TypeOfEstablishment (code)");
            Map(m => m.EstablishmentType.Name).Name("TypeOfEstablishment (name)");
            Map(m => m.EstablishmentTypeGroup.Id).Name("EstablishmentTypeGroup (code)");
            Map(m => m.EstablishmentTypeGroup.Name).Name("EstablishmentTypeGroup (name)");
            Map(m => m.EstablishmentStatus.Id).Name("EstablishmentStatus (code)");
            Map(m => m.EstablishmentStatus.Name).Name("EstablishmentStatus (name)");
            Map(m => m.ReasonEstablishmentOpened.Id).Name("ReasonEstablishmentOpened (code)");
            Map(m => m.ReasonEstablishmentOpened.Name).Name("ReasonEstablishmentOpened (name)");
            Map(m => m.OpenDate).ConvertUsing(m => MapToDate(m, "OpenDate"));
            Map(m => m.ReasonEstablishmentClosed.Id).Name("ReasonEstablishmentClosed (code)");
            Map(m => m.ReasonEstablishmentClosed.Name).Name("ReasonEstablishmentClosed (name)");
            Map(m => m.CloseDate).ConvertUsing(m => MapToDate(m, "CloseDate"));
            Map(m => m.PhaseOfEducation.Id).Name("PhaseOfEducation (code)");
            Map(m => m.PhaseOfEducation.Name).Name("PhaseOfEducation (name)");
            Map(m => m.StatutoryLowAge).ConvertUsing(m => MapToInt(m, "StatutoryLowAge"));
            Map(m => m.StatutoryHighAge).ConvertUsing(m => MapToInt(m, "StatutoryHighAge"));
            Map(m => m.Boarders.Id).Name("Boarders (code)");
            Map(m => m.Boarders.Name).Name("Boarders (name)");
           Map(m => m.NurseryProvision.Id).Name("NurseryProvision (code)");
            Map(m => m.NurseryProvision.Name).Name("NurseryProvision (name)");
            Map(m => m.OfficialSixthForm.Id).Name("OfficialSixthForm (code)");
            Map(m => m.OfficialSixthForm.Name).Name("OfficialSixthForm (name)");
            Map(m => m.Gender.Id).Name("Gender (code)");
            Map(m => m.Gender.Name).Name("Gender (name)");
            Map(m => m.ReligiousCharacter.Id).Name("ReligiousCharacter (code)");
            Map(m => m.ReligiousCharacter.Name).Name("ReligiousCharacter (name)");
            Map(m => m.ReligiousEthos.Id).Name("ReligiousEthos (code)");
            Map(m => m.ReligiousEthos.Name).Name("ReligiousEthos (name)");
            Map(m => m.Diocese.Id).Name("Diocese (code)");
            Map(m => m.Diocese.Name).Name("Diocese (name)");
            Map(m => m.AdmissionsPolicy.Id).Name("AdmissionsPolicy (code)");
            Map(m => m.AdmissionsPolicy.Name).Name("AdmissionsPolicy (name)");
            Map(m => m.SchoolCapacity).ConvertUsing(m => MapToInt(m, "SchoolCapacity"));
            Map(m => m.SpecialClasses.Id).Name("Diocese (code)");
            Map(m => m.SpecialClasses.Name).Name("Diocese (name)");
            Map(m => m.CensusDate).ConvertUsing(m => MapToDate(m, "CensusDate"));
            Map(m => m.NumberOfPupils).ConvertUsing(m => MapToInt(m, "NumberOfPupils"));
            Map(m => m.NumberOfBoys).ConvertUsing(m => MapToInt(m, "NumberOfBoys"));
            Map(m => m.NumberOfGirls).ConvertUsing(m => MapToInt(m, "NumberOfGirls"));
            Map(m => m.PercentageFSM).ConvertUsing(m => MapToDecimal(m, "PercentageFSM"));

             Map(m => m.TrustSchoolFlag.Id).Name("TrustSchoolFlag (code)");
            Map(m => m.TrustSchoolFlag.Name).Name("TrustSchoolFlag (name)");
            Map(m => m.Trusts.Id).Name("Trusts (code)");
            Map(m => m.Trusts.Name).Name("Trusts (name)");

            Map(m => m.SchoolSponsorFlag).Name("SchoolSponsorFlag (name)");
            Map(m => m.SchoolSponsors).Name("SchoolSponsors (name)");
            Map(m => m.FederationFlag).Name("FederationFlag (name)");

            Map(m => m.Federations.Id).Name("Trusts (code)");
            Map(m => m.Federations.Name).Name("Trusts (name)");

            Map(m => m.FEHEIdentifier).Name("FEHEIdentifier");
            Map(m => m.FurtherEducationType).Name("FurtherEducationType (name)");

            Map(m => m.OfstedLastInspectionDate).ConvertUsing(m => MapToDate(m, "OfstedLastInsp"));
            Map(m => m.OfstedSpecialMeasures.Id).Name("OfstedSpecialMeasures (code)");
            Map(m => m.OfstedSpecialMeasures.Name).Name("OfstedSpecialMeasures (name)");
            Map(m => m.OfstedRating).Name("OfstedRating (name)");

            Map(m => m.LastChangedDate).ConvertUsing(m => MapToDate(m, "LastChangedDate"));

            Map(m => m.Street).Name("Street");
            Map(m => m.Locality).Name("Locality");
            Map(m => m.Address3).Name("Address3");
            Map(m => m.Town).Name("Town");
            Map(m => m.County).Name("County");
            Map(m => m.Postcode).Name("Postcode");
            Map(m => m.Country).Name("Country(name)");
            Map(m => m.Easting).ConvertUsing(m => MapToInt(m, "Easting"));
            Map(m => m.Northing).ConvertUsing(m => MapToInt(m, "Northing"));
            Map(m => m.Website).Name("SchoolWebsite");
            Map(m => m.Telephone).Name("TelephoneNum");

            Map(m => m.TeenMoth).Name("TeenMoth (name)");
            Map(m => m.TeenMothPlaces).ConvertUsing(m => MapToInt(m, "TeenMothPlaces"));
            Map(m => m.CCF).Name("CCF (name)");
            Map(m => m.SENPRU).Name("SENPRU (name)");
            Map(m => m.EBD).Name("EBD (name)");
            Map(m => m.PRUPlaces).ConvertUsing(m => MapToInt(m, "PlacesPRU"));

            Map(m => m.FTProv).Name("FTProv (name)");
            Map(m => m.EdByOther).Name("EdByOther (name)");
            Map(m => m.Section41Approved).Name("Section41Approved (name)");
            Map(m => m.SEN1).Name("SEN1 (name)");
            Map(m => m.TypeOfResourcedProvision).Name("TypeOfResourcedProvision (name)");

            Map(m => m.ResourcedProvisionOnRoll).ConvertUsing(m => MapToInt(m, "ResourcedProvisionOnRoll"));
            Map(m => m.ResourcedProvisionCapacity).ConvertUsing(m => MapToInt(m, "ResourcedProvisionCapacity"));
            Map(m => m.SenUnitOnRoll).ConvertUsing(m => MapToInt(m, "SenUnitOnRoll"));
            Map(m => m.SenUnitCapacity).ConvertUsing(m => MapToInt(m, "SenUnitCapacity"));

            Map(m => m.GOR.Id).Name("GOR (code)");
            Map(m => m.GOR.Name).Name("GOR (name)");

            Map(m => m.DistrictAdministrative.Id).Name("DistrictAdministrative (code)");
            Map(m => m.DistrictAdministrative.Name).Name("DistrictAdministrative (name)");

            Map(m => m.AdministrativeWard.Id).Name("AdministrativeWard (code)");
            Map(m => m.AdministrativeWard.Name).Name("AdministrativeWard (name)");

            Map(m => m.ParliamentaryConstituency.Id).Name("ParliamentaryConstituency (code)");
            Map(m => m.ParliamentaryConstituency.Name).Name("ParliamentaryConstituency (name)");

            Map(m => m.UrbanRural.Id).Name("UrbanRural (code)");
            Map(m => m.UrbanRural.Name).Name("UrbanRural (name)");

            Map(m => m.GSSLACode).Name("GSSLACode (name)");
            Map(m => m.CensusAreaStatisticWard).Name("CensusAreaStatisticWard (name)");
            Map(m => m.MSOA).Name("MSOA (name)");
            Map(m => m.LSOA).Name("LSOA (name)");

            Map(m => m.SENStat).ConvertUsing(m => MapToInt(m, "SENStat"));
            Map(m => m.SENNoStat).ConvertUsing(m => MapToInt(m, "SENNoStat"));

            Map(m => m.RSCRegion).Name("RSCRegion (name)");

            //HeadTitle(name),HeadFirstName,HeadLastName,HeadHonours,HeadPreferredJobTitle,InspectorateName(name),InspectorateReport,DateOfLastInspectionVisit,NextInspectionVisit,
            // Inspectorate(name),BoardingEstablishment(name),PropsName,
            // PreviousLA(code),PreviousLA(name),PreviousEstablishmentNumber,
            //RSCRegion(name),SiteName

        }

        private static DateTimeOffset? MapToDate(IReaderRow m, string fieldName)
        {
            var str = m.GetField(fieldName);
            if (DateTimeOffset.TryParse(str, out var date))
            {
                return date;
            }
            return null;
        }

        private static int? MapToInt(IReaderRow m, string fieldName)
        {
            var str = m.GetField(fieldName);
            if (int.TryParse(str, out var number))
            {
                return number;
            }
            return null;
        }

        private static decimal MapToDecimal(IReaderRow m, string fieldName)
        {
            var str = m.GetField(fieldName);
            if (decimal.TryParse(str, out var number))
            {
                return number;
            }
            return decimal.Zero;
        }
    }
}
