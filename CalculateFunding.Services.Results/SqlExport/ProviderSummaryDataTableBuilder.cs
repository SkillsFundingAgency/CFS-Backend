using System;
using System.Data;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class ProviderSummaryDataTableBuilder : DataTableBuilder<ProviderResult>
    {
        private readonly string _specificationIdentifier;

        public ProviderSummaryDataTableBuilder(
            string specificationIdentifier)
        {
            _specificationIdentifier = specificationIdentifier;
        }

        protected override DataColumn[] GetDataColumns(ProviderResult dto)
            => new[]
            {
                NewDataColumn<string>("ProviderId", 128),
                NewDataColumn<string>("ProviderName", 256),
                NewDataColumn<string>("URN", 256, allowNull: true),
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
                NewDataColumn<bool>("IsIndicative", defaultValue: false),
            };

        protected override void AddDataRowToDataTable(ProviderResult dto)
            => DataTable.Rows.Add(
                dto.Provider.Id,
                dto.Provider.Name,
                dto.Provider.URN,
                dto.Provider.UKPRN,
                dto.Provider.UPIN,
                dto.Provider.EstablishmentNumber,
                dto.Provider.DfeEstablishmentNumber,
                dto.Provider.Authority,
                dto.Provider.ProviderType,
                dto.Provider.ProviderSubType,
                dto.Provider.DateOpened?.UtcDateTime,
                dto.Provider.DateClosed?.UtcDateTime,
                dto.Provider.ProviderProfileIdType,
                dto.Provider.LACode,
                dto.Provider.NavVendorNo,
                dto.Provider.CrmAccountId,
                dto.Provider.LegalName,
                dto.Provider.Status,
                dto.Provider.PhaseOfEducation,
                dto.Provider.ReasonEstablishmentOpened,
                dto.Provider.ReasonEstablishmentClosed,
                dto.Provider.TrustStatus,
                dto.Provider.TrustName,
                dto.Provider.TrustCode,
                dto.Provider.Town,
                dto.Provider.Postcode,
                dto.Provider.CompaniesHouseNumber,
                dto.Provider.GroupIdNumber,
                dto.Provider.RscRegionName,
                dto.Provider.RscRegionCode,
                dto.Provider.GovernmentOfficeRegionName,
                dto.Provider.GovernmentOfficeRegionCode,
                dto.Provider.DistrictName,
                dto.Provider.DistrictCode,
                dto.Provider.WardName,
                dto.Provider.WardCode,
                dto.Provider.CensusWardName,
                dto.Provider.CensusWardCode,
                dto.Provider.MiddleSuperOutputAreaName,
                dto.Provider.MiddleSuperOutputAreaCode,
                dto.Provider.LowerSuperOutputAreaName,
                dto.Provider.LowerSuperOutputAreaCode,
                dto.Provider.ParliamentaryConstituencyName,
                dto.Provider.ParliamentaryConstituencyCode,
                dto.Provider.CountryCode,
                dto.Provider.CountryName,
                dto.Provider.LocalGovernmentGroupTypeCode,
                dto.Provider.LocalGovernmentGroupTypeName,
                dto.Provider.Street,
                dto.Provider.Locality,
                dto.Provider.Address3,
                dto.Provider.PaymentOrganisationIdentifier,
                dto.Provider.PaymentOrganisationName,
                dto.Provider.ProviderTypeCode,
                dto.Provider.ProviderSubTypeCode,
                dto.Provider.PreviousLaCode,
                dto.Provider.PreviousLaName,
                dto.Provider.PreviousEstablishmentNumber,
                dto.Provider.FurtherEducationTypeCode,
                dto.Provider.FurtherEducationTypeName,
                string.Join(";", dto.Provider.Predecessors?.Select(s => s) ?? Array.Empty<string>()),
                string.Join(";", dto.Provider.Successors?.Select(s => s) ?? Array.Empty<string>()),
                dto.IsIndicativeProvider);

        protected override void EnsureTableNameIsSet(ProviderResult dto)
            => TableName = $"[dbo].[{_specificationIdentifier}_Providers]";
    }
}