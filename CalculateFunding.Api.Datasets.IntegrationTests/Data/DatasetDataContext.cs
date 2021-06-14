using System.Reflection;
using CalculateFunding.IntegrationTests.Common.Data;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Data
{
    public class DatasetDataContext : NoPartitionKeyCosmosBulkDataContext
    {
        public DatasetDataContext(IConfiguration configuration,
            Assembly resourceAssembly) : base(configuration,
            "datasets",
            "CalculateFunding.Api.Datasets.IntegrationTests.Resources.DatasetTemplate",
            resourceAssembly)
        {
        }

        protected override object GetFormatParametersForDocument(dynamic documentData,
            string now) =>
            new
            {
                ID = documentData.Id,
                DESCRIPTION = documentData.Description,
                VERSION = documentData.Version,
                AUTHORID = documentData.AuthorId,
                AUTHORNAME = documentData.AuthorName,
                BLOBNAME = documentData.BlobName,
                UPLOADEDBLOBFILEPATH = documentData.UploadedBlobPath,
                CHANGETYPE = documentData.ChangeType.ToString(),
                CONVERTERWIZARD = documentData.ConverterWizard.ToString().ToLower(),
                DEFINITIONID = documentData.DefinitionId,
                DEFINITIONNAME = documentData.DefinitionName,
                PUBLISHSTATUS = documentData.PublishStatus.ToString(),
                ROWCOUNT = documentData.RowCount,
                AMENDEDROWCOUNT = documentData.AmendedRowCount,
                NEWROWCOUNT = documentData.NewRowCount,
                FUNDINGSTREAMID = documentData.FundingStreamId,
                FUNDINGSTREAMNAME = documentData.FundingStreamName,
                FUNDINGSTREAMSHORTNAME = documentData.FundingStreamShortName,
                NOW = now,
                PROVIDERVERSIONID = documentData.ProviderVersionId
            };
    }
}