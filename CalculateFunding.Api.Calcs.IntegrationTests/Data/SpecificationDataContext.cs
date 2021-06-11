using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Calcs.IntegrationTests.Data
{
    public class SpecificationDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public SpecificationDataContext(IConfiguration configuration) : base(configuration,
            "specs",
            "CalculateFunding.Api.Calcs.IntegrationTests.Resources.SpecificationTemplate",
            typeof(SpecificationDataContext).Assembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                SPECIFICATIONVERSIONID = documentData.SpecificationVersionId,
                NAME = documentData.Name,
                FUNDINGPERIODID = documentData.FundingPeriodId,
                FUNDINGPERIODNAME = documentData.FundingPeriodName,
                DESCRIPTION = documentData.Description,
                DATADEFINITIONRELATIONSHIPIDS = ((string[])documentData.DataDefinitionRelationshipIds).AsJson(),
                TEMPLATEIDS = ((IDictionary<string, string>)documentData.TemplateIds).AsJson() ?? "null",
                PROVIDERSOURCE = documentData.ProviderSource.ToString(),
                PROVIDERSNAPSHOTID = documentData.ProviderSnapshotId,
                VERSION = documentData.Version,
                AUTHORID = documentData.AuthorId,
                AUTHORNAME = documentData.AuthorName,
                PUBLISHSTATUS = documentData.PublishStatus,
                FUNDINGSTREAMS = ((Reference[])documentData.FundingStreams).AsJson(),
                NOW = now,
                PROVIDERVERSIONID = documentData.ProviderVersionId,
                ISSELECTEDFORFUNDING = documentData.IsSelectedForFunding.ToString().ToLower()
            };
    }
}