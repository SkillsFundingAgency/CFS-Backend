﻿using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.SqlExport;
using System;
using System.Data;


namespace CalculateFunding.Services.Publishing.FundingManagement
{
    public class FundingGroupVersionDataTableBuilder : DataTableBuilder<FundingGroupVersion>
    {
        protected override DataColumn[] GetDataColumns(FundingGroupVersion dto) =>
            new[]
            {
                NewDataColumn<Guid>("FundingGroupVersionId"),
                NewDataColumn<Guid>("FundingGroupId"),
                NewDataColumn<int>("ChannelId"),
                NewDataColumn<DateTime>("StatusChangedDate"),
                NewDataColumn<int>("MajorVersion"),
                NewDataColumn<int>("MinorVersion"),
                NewDataColumn<string>("TemplateVersion", 64),
                NewDataColumn<string>("SchemaVersion", 64),
                NewDataColumn<string>("JobId", 64),
                NewDataColumn<string>("CorrelationId", 64),
                NewDataColumn<int>("FundingPeriodId"),
                NewDataColumn<int>("FundingStreamId"),
                NewDataColumn<string>("FundingId", 128),
                NewDataColumn<int>("GroupingReasonId"),
                NewDataColumn<decimal>("TotalFunding"),
                NewDataColumn<DateTime>("ExternalPublicationDate"),
                NewDataColumn<DateTime>("EarliestPaymentAvailableDate"),
                NewDataColumn<int>("ChannelVersion"),
            };

        protected override void AddDataRowToDataTable(FundingGroupVersion dto)
        {
            DataTable.Rows.Add(dto.FundingGroupVersionId,
                dto.FundingGroupId,
                dto.ChannelId,
                dto.StatusChangedDate,
                dto.MajorVersion,
                dto.MinorVersion,
                dto.TemplateVersion,
                dto.SchemaVersion,
                dto.JobId,
                dto.CorrelationId,
                dto.FundingPeriodId,
                dto.FundingStreamId,
                dto.FundingId,
                dto.GroupingReasonId,
                dto.TotalFunding,
                dto.ExternalPublicationDate,
                dto.EarliestPaymentAvailableDate,
                dto.ChannelVersion);
        }

        protected override void EnsureTableNameIsSet(FundingGroupVersion dto)
            => TableName = $"[dbo].[FundingGroupVersions]";
    }
}