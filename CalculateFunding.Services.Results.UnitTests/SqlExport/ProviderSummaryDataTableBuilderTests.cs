using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class ProviderSummaryDataTableBuilderTests : DataTableBuilderTest<ProviderSummaryDataTableBuilder>
    {
        private string specificationName;
        private string templateVersion;
        private string providerVersion;


        [TestInitialize]
        public void SetUp()
        {
            FundingStreamId = NewRandomStringWithMaxLength(32);
            FundingPeriodId = NewRandomStringWithMaxLength(32);
            SpecificationId = NewRandomStringWithMaxLength(32);

            specificationName = NewRandomString();
            templateVersion = NewRandomString();
            providerVersion = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithFundingStreamIds(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId)
                .WithName(specificationName)
                .WithTemplateIds((FundingStreamId, templateVersion))
                .WithProviderVersionId(providerVersion));

            DataTableBuilder = new ProviderSummaryDataTableBuilder();
        }

        [TestMethod]
        public void MapsTemplateCalculationsIntoDataTable()
        {
            ProviderResult rowOne = NewProviderResult(_ => _.WithSpecificationId(SpecificationId)
                .WithProviderSummary(
                    NewProviderSummary(
                        )));

            WhenTheRowsAreAdded(rowOne);

            ThenTheDataTableHasColumnsMatching(
                NewDataColumn<string>("ProviderId", 128),
                NewDataColumn<string>("ProviderName", 256),
                NewDataColumn<string>("URN", 256),
                NewDataColumn<string>("UKPRN", 256, allowNull: true),
                NewDataColumn<string>("UPIN", 256, allowNull: true),
                NewDataColumn<string>("EstablishmentNumber", 256, allowNull: true),
                NewDataColumn<string>("DfeEstablishmentNumber", 256, allowNull: true),
                NewDataColumn<string>("Authority", 256, allowNull: true),
                NewDataColumn<string>("ProviderType", 256, allowNull: true),
                NewDataColumn<string>("ProviderSubType", 256, allowNull: true),
                NewDataColumn<DateTime>("DateOpened", allowNull: true),
                NewDataColumn<DateTime>("DateClosed", allowNull: true),
                NewDataColumn<string>("ProviderProfileIdType", 256, allowNull: true),
                NewDataColumn<string>("LACode", 256, allowNull: true),
                NewDataColumn<string>("NavVendorNo", 256, allowNull: true),
                NewDataColumn<string>("CrmAccountId", 256, allowNull: true),
                NewDataColumn<string>("LegalName", 256, allowNull: true),
                NewDataColumn<string>("Status", 256, allowNull: true),
                NewDataColumn<string>("PhaseOfEducation", 256, allowNull: true),
                NewDataColumn<string>("ReasonEstablishmentOpened", 256, allowNull: true),
                NewDataColumn<string>("ReasonEstablishmentClosed", 256, allowNull: true),
                NewDataColumn<string>("TrustStatus", 256, allowNull: true),
                NewDataColumn<string>("TrustName", 256, allowNull: true),
                NewDataColumn<string>("TrustCode", 256, allowNull: true),
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
                NewDataColumn<string>("CountryCode", 256, allowNull: true),
                NewDataColumn<string>("CountryName", 256, allowNull: true),
                NewDataColumn<string>("LocalGovernmentGroupTypeCode", 256, allowNull: true),
                NewDataColumn<string>("LocalGovernmentGroupTypeName", 256, allowNull: true),
                NewDataColumn<string>("Street", 256, allowNull: true),
                NewDataColumn<string>("Locality", 256, allowNull: true),
                NewDataColumn<string>("Address3", 256, allowNull: true),
                NewDataColumn<string>("PaymentOrganisationIdentifier", 256, allowNull: true),
                NewDataColumn<string>("PaymentOrganisationName", 256, allowNull: true),
                NewDataColumn<string>("ProviderTypeCode", 256, allowNull: true),
                NewDataColumn<string>("ProviderSubTypeCode", 256, allowNull: true),
                NewDataColumn<string>("PreviousLaCode", 256, allowNull: true),
                NewDataColumn<string>("PreviousLaName", 256, allowNull: true),
                NewDataColumn<string>("PreviousEstablishmentNumber", 256, allowNull: true),
                NewDataColumn<string>("FurtherEducationTypeCode", 256, allowNull: true),
                NewDataColumn<string>("FurtherEducationTypeName", 256, allowNull: true),
                NewDataColumn<string>("Predecessors", 256, allowNull: true),
                NewDataColumn<string>("Successors", 256, allowNull: true),
                NewDataColumn<bool>("IsIndicative"));
            AndTheDataTableHasRowsMatching(
                NewRow(
                    rowOne.Provider.Id,
                    rowOne.Provider.Name,
                    rowOne.Provider.URN,
                    rowOne.Provider.UKPRN,
                    rowOne.Provider.UPIN,
                    rowOne.Provider.EstablishmentNumber,
                    rowOne.Provider.DfeEstablishmentNumber,
                    rowOne.Provider.Authority,
                    rowOne.Provider.ProviderType,
                    rowOne.Provider.ProviderSubType,
                    rowOne.Provider.DateOpened?.UtcDateTime,
                    rowOne.Provider.DateClosed?.UtcDateTime,
                    rowOne.Provider.ProviderProfileIdType,
                    rowOne.Provider.LACode,
                    rowOne.Provider.NavVendorNo,
                    rowOne.Provider.CrmAccountId,
                    rowOne.Provider.LegalName,
                    rowOne.Provider.Status,
                    rowOne.Provider.PhaseOfEducation,
                    rowOne.Provider.ReasonEstablishmentOpened,
                    rowOne.Provider.ReasonEstablishmentClosed,
                    rowOne.Provider.TrustStatus,
                    rowOne.Provider.TrustName,
                    rowOne.Provider.TrustCode,
                    rowOne.Provider.Town,
                    rowOne.Provider.Postcode,
                    rowOne.Provider.CompaniesHouseNumber,
                    rowOne.Provider.GroupIdNumber,
                    rowOne.Provider.RscRegionName,
                    rowOne.Provider.RscRegionCode,
                    rowOne.Provider.GovernmentOfficeRegionName,
                    rowOne.Provider.GovernmentOfficeRegionCode,
                    rowOne.Provider.DistrictName,
                    rowOne.Provider.DistrictCode,
                    rowOne.Provider.WardName,
                    rowOne.Provider.WardCode,
                    rowOne.Provider.CensusWardName,
                    rowOne.Provider.CensusWardCode,
                    rowOne.Provider.MiddleSuperOutputAreaName,
                    rowOne.Provider.MiddleSuperOutputAreaCode,
                    rowOne.Provider.LowerSuperOutputAreaName,
                    rowOne.Provider.LowerSuperOutputAreaCode,
                    rowOne.Provider.ParliamentaryConstituencyName,
                    rowOne.Provider.ParliamentaryConstituencyCode,
                    rowOne.Provider.CountryCode,
                    rowOne.Provider.CountryName,
                    rowOne.Provider.LocalGovernmentGroupTypeCode,
                    rowOne.Provider.LocalGovernmentGroupTypeName,
                    rowOne.Provider.Street,
                    rowOne.Provider.Locality,
                    rowOne.Provider.Address3,
                    rowOne.Provider.PaymentOrganisationIdentifier,
                    rowOne.Provider.PaymentOrganisationName,
                    rowOne.Provider.ProviderTypeCode,
                    rowOne.Provider.ProviderSubTypeCode,
                    rowOne.Provider.PreviousLaCode,
                    rowOne.Provider.PreviousLaName,
                    rowOne.Provider.PreviousEstablishmentNumber,
                    rowOne.Provider.FurtherEducationTypeCode,
                    rowOne.Provider.FurtherEducationTypeName,
                    string.Join(";", rowOne.Provider.Predecessors?.Select(s => s) ?? Array.Empty<string>()),
                    string.Join(";", rowOne.Provider.Successors?.Select(s => s) ?? Array.Empty<string>()),
                    rowOne.IsIndicativeProvider));
            AndTheTableNameIs($"[dbo].[{SpecificationId}_Providers]");
        }
    }
}
