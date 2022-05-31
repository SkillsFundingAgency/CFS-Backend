using System;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class ProviderDataTableBuilderTests : DataTableBuilderTest<ProviderDataTableBuilder>
    {
        [TestInitialize]
        public void SetUp()
        {
            DataTableBuilder = new ProviderDataTableBuilder();       
        }
        
        [TestMethod]
        public void MapsPaymentFundingLinesIntoDataTable()
        {
            PublishedProviderVersion rowOne = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomString().Substring(0, 32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithProvider(NewProvider(prov => 
                    prov.WithUKPRN(NewRandomStringWithMaxLength(32))
                        .WithURN(NewRandomStringWithMaxLength(32))
                        .WithUPIN(NewRandomStringWithMaxLength(32))
                        .WithLACode(NewRandomStringWithMaxLength(32))
                        .WithSuccessor(NewRandomStringWithMaxLength(32))
                        .WithTrustCode(NewRandomStringWithMaxLength(32))
                        .WithPaymentOrganisationIdentifier(NewRandomStringWithMaxLength(32)))));
            PublishedProviderVersion rowTwo = NewPublishedProviderVersion(_ => _.WithFundingStreamId(FundingStreamId)
                .WithProviderId(NewRandomStringWithMaxLength(32))
                .WithFundingPeriodId(FundingPeriodId)
                .WithIsIndicative(false)
                .WithProvider(NewProvider(prov => 
                    prov.WithUKPRN(NewRandomStringWithMaxLength(32))
                        .WithURN(NewRandomStringWithMaxLength(32))
                        .WithUPIN(NewRandomStringWithMaxLength(32))
                        .WithLACode(NewRandomStringWithMaxLength(32))
                        .WithSuccessor(NewRandomStringWithMaxLength(32))
                        .WithTrustCode(NewRandomStringWithMaxLength(32))
                        .WithPaymentOrganisationIdentifier(NewRandomStringWithMaxLength(32)))));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(NewDataColumn<string>("PublishedProviderId", 128),
                NewDataColumn<string>("ProviderId", 32),
                NewDataColumn<string>("ProviderName", 256),
                NewDataColumn<string>("UKPRN", 32),
                NewDataColumn<string>("URN", 32),
                NewDataColumn<string>("Authority", 256),
                NewDataColumn<string>("ProviderType", 128),
                NewDataColumn<string>("ProviderSubType", 128),
                NewDataColumn<DateTime>("DateOpened", allowNull: true),
                NewDataColumn<DateTime>("DateClosed", allowNull: true),
                NewDataColumn<string>("LaCode", 32),
                NewDataColumn<string>("Status", 64, true),
                NewDataColumn<string>("Successor", 32, true),
                NewDataColumn<string>("TrustCode", 32, allowNull: true),
                NewDataColumn<string>("TrustName", 128, allowNull: true),
                NewDataColumn<string>("PaymentOrganisationIdentifier", 32, true),
                NewDataColumn<string>("PaymentOrganisationName", 256, true),
                NewDataColumn<bool>("IsIndicative"),
                NewDataColumn<string>("TrustStatus", 256, allowNull: true),
                NewDataColumn<string>("UPIN", 32, allowNull: true),
                NewDataColumn<string>("EstablishmentNumber", 256, allowNull: true),
                NewDataColumn<string>("DfeEstablishmentNumber", 256, allowNull: true),
                NewDataColumn<string>("ProviderProfileIdType", 256, allowNull: true),
                NewDataColumn<string>("NavVendorNo", 256, allowNull: true),
                NewDataColumn<string>("CrmAccountId", 256, allowNull: true),
                NewDataColumn<string>("LegalName", 256, allowNull: true),
                NewDataColumn<string>("PhaseOfEducation", 256, allowNull: true),
                NewDataColumn<string>("ReasonEstablishmentOpened", 256, allowNull: true),
                NewDataColumn<string>("ReasonEstablishmentClosed", 256, allowNull: true),
                NewDataColumn<string>("Predecessors", 256, allowNull: true),
                NewDataColumn<string>("Successors", 256, allowNull: true),
                NewDataColumn<string>("Town", 256, allowNull: true),
                NewDataColumn<string>("Postcode", 256, allowNull: true),
                NewDataColumn<string>("CompaniesHouseNumber", 256, allowNull: true),
                NewDataColumn<string>("GroupIdNumber", 256, allowNull: true),
                NewDataColumn<string>("RscRegionName", 256, allowNull: true),
                NewDataColumn<string>("RscRegionCode", 256, allowNull: true),
                NewDataColumn<string>("GovernmentOfficeRegionName", 256, allowNull: true),
                NewDataColumn<string>("GovernmentOfficeRegionCode", 256, allowNull: true),
                NewDataColumn<string>("DistrictName", 256, allowNull: true),
                NewDataColumn<string>("DistrictCode", 256, allowNull: true),
                NewDataColumn<string>("WardName", 256, allowNull: true),
                NewDataColumn<string>("WardCode", 256, allowNull: true),
                NewDataColumn<string>("CensusWardName", 256, allowNull: true),
                NewDataColumn<string>("CensusWardCode", 256, allowNull: true),
                NewDataColumn<string>("MiddleSuperOutputAreaName", 256, allowNull: true),
                NewDataColumn<string>("MiddleSuperOutputAreaCode", 256, allowNull: true),
                NewDataColumn<string>("LowerSuperOutputAreaName", 256, allowNull: true),
                NewDataColumn<string>("LowerSuperOutputAreaCode", 256, allowNull: true),
                NewDataColumn<string>("ParliamentaryConstituencyName", 256, allowNull: true),
                NewDataColumn<string>("ParliamentaryConstituencyCode", 256, allowNull: true),
                NewDataColumn<string>("LondonRegionCode", 256, allowNull: true),
                NewDataColumn<string>("LondonRegionName", 256, allowNull: true),
                NewDataColumn<string>("CountryCode", 256, allowNull: true),
                NewDataColumn<string>("CountryName", 256, allowNull: true),
                NewDataColumn<string>("LocalGovernmentGroupTypeCode", 256, allowNull: true),
                NewDataColumn<string>("LocalGovernmentGroupTypeName", 256, allowNull: true),
                NewDataColumn<string>("Street", 256, allowNull: true),
                NewDataColumn<string>("Locality", 256, allowNull: true),
                NewDataColumn<string>("Address3", 256, allowNull: true),
                NewDataColumn<string>("ProviderTypeCode", 256, allowNull: true),
                NewDataColumn<string>("ProviderSubTypeCode", 256, allowNull: true),
                NewDataColumn<string>("PreviousLaCode", 256, allowNull: true),
                NewDataColumn<string>("PreviousLaName", 256, allowNull: true),
                NewDataColumn<string>("PreviousEstablishmentNumber", 256, allowNull: true),
                NewDataColumn<string>("FurtherEducationTypeCode", 256, allowNull: true),
                NewDataColumn<string>("FurtherEducationTypeName", 256, allowNull: true));
            
            Provider rowOneProvider = rowOne.Provider;
            Provider rowTwoProvider = rowTwo.Provider;
            
            AndTheDataTableHasRowsMatching(NewRow(
                rowOne.PublishedProviderId,
                rowOne.ProviderId,
                rowOneProvider.Name,
                rowOneProvider.UKPRN,
                rowOneProvider.URN,
                rowOneProvider.Authority,
                rowOneProvider.ProviderType,
                rowOneProvider.ProviderSubType,
                DbNullSafe(rowOneProvider.DateOpened?.UtcDateTime),
                DbNullSafe(rowOneProvider.DateClosed?.UtcDateTime),
                rowOneProvider.LACode,
                DbNullSafe(rowOneProvider.Status),
                DbNullSafe(rowOneProvider.Successor),
                DbNullSafe(rowOneProvider.TrustCode),
                DbNullSafe(rowOneProvider.TrustName),
                DbNullSafe(rowOneProvider.PaymentOrganisationIdentifier),
                DbNullSafe(rowOneProvider.PaymentOrganisationName),
                rowOne.IsIndicative,
                DbNullSafe(rowOneProvider.TrustStatus),
                DbNullSafe(rowOneProvider.UPIN),
                DbNullSafe(rowOneProvider.EstablishmentNumber),
                DbNullSafe(rowOneProvider.DfeEstablishmentNumber),
                DbNullSafe(rowOneProvider.ProviderProfileIdType),
                DbNullSafe(rowOneProvider.NavVendorNo),
                DbNullSafe(rowOneProvider.CrmAccountId),
                DbNullSafe(rowOneProvider.LegalName),
                DbNullSafe(rowOneProvider.PhaseOfEducation),
                DbNullSafe(rowOneProvider.ReasonEstablishmentOpened),
                DbNullSafe(rowOneProvider.ReasonEstablishmentClosed),
                DbNullSafe(rowOneProvider.Predecessors?.JoinWith(';')),
                DbNullSafe(rowOneProvider.Successors?.JoinWith(';')),
                DbNullSafe(rowOneProvider.Town),
                DbNullSafe(rowOneProvider.Postcode),
                DbNullSafe(rowOneProvider.CompaniesHouseNumber),
                DbNullSafe(rowOneProvider.GroupIdNumber),
                DbNullSafe(rowOneProvider.RscRegionName),
                DbNullSafe(rowOneProvider.RscRegionCode),
                DbNullSafe(rowOneProvider.GovernmentOfficeRegionName),
                DbNullSafe(rowOneProvider.GovernmentOfficeRegionCode),
                DbNullSafe(rowOneProvider.DistrictName),
                DbNullSafe(rowOneProvider.DistrictCode),
                DbNullSafe(rowOneProvider.WardName),
                DbNullSafe(rowOneProvider.WardCode),
                DbNullSafe(rowOneProvider.CensusWardName),
                DbNullSafe(rowOneProvider.CensusWardCode),
                DbNullSafe(rowOneProvider.MiddleSuperOutputAreaName),
                DbNullSafe(rowOneProvider.MiddleSuperOutputAreaCode),
                DbNullSafe(rowOneProvider.LowerSuperOutputAreaName),
                DbNullSafe(rowOneProvider.LowerSuperOutputAreaCode),
                DbNullSafe(rowOneProvider.ParliamentaryConstituencyName),
                DbNullSafe(rowOneProvider.ParliamentaryConstituencyCode),
                DbNullSafe(rowOneProvider.LondonRegionCode),
                DbNullSafe(rowOneProvider.LondonRegionName),
                DbNullSafe(rowOneProvider.CountryCode),
                DbNullSafe(rowOneProvider.CountryName),
                DbNullSafe(rowOneProvider.LocalGovernmentGroupTypeCode),
                DbNullSafe(rowOneProvider.LocalGovernmentGroupTypeName),
                DbNullSafe(rowOneProvider.Street),
                DbNullSafe(rowOneProvider.Locality),
                DbNullSafe(rowOneProvider.Address3),
                DbNullSafe(rowOneProvider.ProviderTypeCode),
                DbNullSafe(rowOneProvider.ProviderSubTypeCode),
                DbNullSafe(rowOneProvider.PreviousLaCode),
                DbNullSafe(rowOneProvider.PreviousLaName),
                DbNullSafe(rowOneProvider.PreviousEstablishmentNumber),
                DbNullSafe(rowOneProvider.FurtherEducationTypeCode),
                DbNullSafe(rowOneProvider.FurtherEducationTypeName)),
                NewRow(rowTwo.PublishedProviderId,
                    rowTwo.ProviderId,
                    rowTwoProvider.Name,
                    rowTwoProvider.UKPRN,
                    rowTwoProvider.URN,
                    rowTwoProvider.Authority,
                    rowTwoProvider.ProviderType,
                    rowTwoProvider.ProviderSubType,
                    DbNullSafe(rowTwoProvider.DateOpened?.UtcDateTime),
                    DbNullSafe(rowTwoProvider.DateClosed?.UtcDateTime),
                    rowTwoProvider.LACode,
                    DbNullSafe(rowTwoProvider.Status),
                    DbNullSafe(rowTwoProvider.Successor),
                    DbNullSafe(rowTwoProvider.TrustCode),
                    DbNullSafe(rowTwoProvider.TrustName),
                    DbNullSafe(rowTwoProvider.PaymentOrganisationIdentifier),
                    DbNullSafe(rowTwoProvider.PaymentOrganisationName),
                    rowTwo.IsIndicative,
                    DbNullSafe(rowTwoProvider.TrustStatus),
                    DbNullSafe(rowTwoProvider.UPIN),
                    DbNullSafe(rowTwoProvider.EstablishmentNumber),
                    DbNullSafe(rowTwoProvider.DfeEstablishmentNumber),
                    DbNullSafe(rowTwoProvider.ProviderProfileIdType),
                    DbNullSafe(rowTwoProvider.NavVendorNo),
                    DbNullSafe(rowTwoProvider.CrmAccountId),
                    DbNullSafe(rowTwoProvider.LegalName),
                    DbNullSafe(rowTwoProvider.PhaseOfEducation),
                    DbNullSafe(rowTwoProvider.ReasonEstablishmentOpened),
                    DbNullSafe(rowTwoProvider.ReasonEstablishmentClosed),
                    DbNullSafe(rowTwoProvider.Predecessors?.JoinWith(';')),
                    DbNullSafe(rowTwoProvider.Successors?.JoinWith(';')),
                    DbNullSafe(rowTwoProvider.Town),
                    DbNullSafe(rowTwoProvider.Postcode),
                    DbNullSafe(rowTwoProvider.CompaniesHouseNumber),
                    DbNullSafe(rowTwoProvider.GroupIdNumber),
                    DbNullSafe(rowTwoProvider.RscRegionName),
                    DbNullSafe(rowTwoProvider.RscRegionCode),
                    DbNullSafe(rowTwoProvider.GovernmentOfficeRegionName),
                    DbNullSafe(rowTwoProvider.GovernmentOfficeRegionCode),
                    DbNullSafe(rowTwoProvider.DistrictName),
                    DbNullSafe(rowTwoProvider.DistrictCode),
                    DbNullSafe(rowTwoProvider.WardName),
                    DbNullSafe(rowTwoProvider.WardCode),
                    DbNullSafe(rowTwoProvider.CensusWardName),
                    DbNullSafe(rowTwoProvider.CensusWardCode),
                    DbNullSafe(rowTwoProvider.MiddleSuperOutputAreaName),
                    DbNullSafe(rowTwoProvider.MiddleSuperOutputAreaCode),
                    DbNullSafe(rowTwoProvider.LowerSuperOutputAreaName),
                    DbNullSafe(rowTwoProvider.LowerSuperOutputAreaCode),
                    DbNullSafe(rowTwoProvider.ParliamentaryConstituencyName),
                    DbNullSafe(rowTwoProvider.ParliamentaryConstituencyCode),
                    DbNullSafe(rowTwoProvider.LondonRegionCode),
                    DbNullSafe(rowTwoProvider.LondonRegionName),
                    DbNullSafe(rowTwoProvider.CountryCode),
                    DbNullSafe(rowTwoProvider.CountryName),
                    DbNullSafe(rowTwoProvider.LocalGovernmentGroupTypeCode),
                    DbNullSafe(rowTwoProvider.LocalGovernmentGroupTypeName),
                    DbNullSafe(rowTwoProvider.Street),
                    DbNullSafe(rowTwoProvider.Locality),
                    DbNullSafe(rowTwoProvider.Address3),
                    DbNullSafe(rowTwoProvider.ProviderTypeCode),
                    DbNullSafe(rowTwoProvider.ProviderSubTypeCode),
                    DbNullSafe(rowTwoProvider.PreviousLaCode),
                    DbNullSafe(rowTwoProvider.PreviousLaName),
                    DbNullSafe(rowTwoProvider.PreviousEstablishmentNumber),
                    DbNullSafe(rowTwoProvider.FurtherEducationTypeCode),
                    DbNullSafe(rowTwoProvider.FurtherEducationTypeName)));
            AndTheTableNameIs($"[dbo].[{FundingStreamId}_{FundingPeriodId}_Providers]");
        }
    }
}