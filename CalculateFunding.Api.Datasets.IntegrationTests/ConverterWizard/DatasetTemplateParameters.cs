using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class DatasetTemplateParameters
    {
        public string Id { get; set; }

        public string DefinitionId { get; set; }

        public string DefinitionName { get; set; }

        public string Description { get; set; }

        public bool ConverterWizard { get; set; }

        public string BlobName { get; set; }

        public int NewRowCount { get; set; }

        public int RowCount { get; set; }

        public int AmendedRowCount { get; set; }

        public string UploadedBlobPath { get; set; }

        public DatasetChangeType? ChangeType { get; set; }

        public string FundingStreamShortName { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingStreamName { get; set; }

        public int Version { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        public PublishStatus? PublishStatus { get; set; }
    }
}