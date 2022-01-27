using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System.Data;

namespace CalculateFunding.Services.Publishing.FundingManagement.Migration
{
    public class FundingGroupDataTableBuilder : DataTableBuilder<FundingGroup>
    {
        protected override DataColumn[] GetDataColumns(FundingGroup dto) =>
            new[]
            {
                NewDataColumn<int>("FundingGroupId"),
                NewDataColumn<string>("SpecificationId", 64),
                NewDataColumn<int>("ChannelId"),
                NewDataColumn<string>("OrganisationGroupTypeCode", 128),
                NewDataColumn<string>("OrganisationGroupTypeIdentifier", 128),
                NewDataColumn<string>("OrganisationGroupIdentifierValue", 128),
                NewDataColumn<string>("OrganisationGroupName", 256),
                NewDataColumn<string>("OrganisationGroupSearchableName", 256),
                NewDataColumn<string>("OrganisationGroupTypeClassification", 128),
                NewDataColumn<int>("GroupingReasonId")
            };

        protected override void AddDataRowToDataTable(FundingGroup dto)
        {
            DataTable.Rows.Add(dto.FundingGroupId,
                dto.SpecificationId,
                dto.ChannelId,
                dto.OrganisationGroupTypeCode,
                dto.OrganisationGroupTypeIdentifier,
                dto.OrganisationGroupIdentifierValue,
                dto.OrganisationGroupName,
                dto.OrganisationGroupSearchableName,
                dto.OrganisationGroupTypeClassification,
                dto.GroupingReasonId);
        }

        protected override void EnsureTableNameIsSet(FundingGroup dto)
            => TableName = $"[dbo].[FundingGroups]";
    }
}
