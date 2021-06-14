using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class DatasetTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private int? _definitionVersion;
        private string _definitionId;
        private string _definitionName;
        private string _description;
        private bool _converterWizard;
        private string _blobName;
        private int? _newRowCount;
        private int? _rowCount;
        private int? _amendedRowCount;
        private string _uploadedBlobPath;
        private DatasetChangeType? _changeType;
        private string _fundingStreamShortName;
        private string _fundingStreamId;
        private string _fundingStreamName;
        private int? _version;
        private string _authorId;
        private string _authorName;
        private PublishStatus? _publishStatus;
        private string _providerVersionId;

        public DatasetTemplateParametersBuilder WithPublishStatus(PublishStatus publishStatus)
        {
            _publishStatus = publishStatus;

            return this;
        }

        public DatasetTemplateParametersBuilder WithAuthorId(string authorId)
        {
            _authorId = authorId;

            return this;
        }

        public DatasetTemplateParametersBuilder WithAuthorName(string authorName)
        {
            _authorName = authorName;

            return this;
        }

        public DatasetTemplateParametersBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetTemplateParametersBuilder WithUploadedBlobPath(string uploadedBlobPath)
        {
            _uploadedBlobPath = uploadedBlobPath;

            return this;
        }

        public DatasetTemplateParametersBuilder WithChangeType(DatasetChangeType datasetChangeType)
        {
            _changeType = datasetChangeType;

            return this;
        }

        public DatasetTemplateParametersBuilder WithFundingStreamShortName(string fundingStreamShortName)
        {
            _fundingStreamShortName = fundingStreamShortName;

            return this;
        }

        public DatasetTemplateParametersBuilder WithFundingStreamName(string fundingStreamName)
        {
            _fundingStreamName = fundingStreamName;

            return this;
        }

        public DatasetTemplateParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public DatasetTemplateParametersBuilder WithRowCount(int rowCount)
        {
            _rowCount = rowCount;

            return this;
        }

        public DatasetTemplateParametersBuilder WithAmendedRowCount(int amendedRowCount)
        {
            _amendedRowCount = amendedRowCount;

            return this;
        }

        public DatasetTemplateParametersBuilder WithBlobName(string blobName)
        {
            _blobName = blobName;

            return this;
        }

        public DatasetTemplateParametersBuilder WithNewRowCount(int newRowCount)
        {
            _newRowCount = newRowCount;

            return this;
        }

        public DatasetTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DatasetTemplateParametersBuilder WithDefinitionId(string definitionId)
        {
            _definitionId = definitionId;

            return this;
        }

        public DatasetTemplateParametersBuilder WithDefinitionName(string definitionName)
        {
            _definitionName = definitionName;

            return this;
        }

        public DatasetTemplateParametersBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DatasetTemplateParametersBuilder WithConverterWizard(bool converterWizard)
        {
            _converterWizard = converterWizard;

            return this;
        }

        public DatasetTemplateParametersBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public DatasetTemplateParameters Build() =>
            new DatasetTemplateParameters
            {
                Id = _id ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Version = _version.GetValueOrDefault(),
                AuthorId = _authorId ?? NewRandomString(),
                AuthorName = _authorName ?? NewRandomString(),
                BlobName = _blobName,
                UploadedBlobPath = _uploadedBlobPath,
                ChangeType = _changeType.GetValueOrDefault(NewRandomEnum<DatasetChangeType>()),
                ConverterWizard = _converterWizard,
                DefinitionId = _definitionId ?? NewRandomString(),
                DefinitionName = _definitionName ?? NewRandomString(),
                PublishStatus = _publishStatus.GetValueOrDefault(NewRandomEnum<PublishStatus>()),
                RowCount = _rowCount.GetValueOrDefault(),
                AmendedRowCount = _amendedRowCount.GetValueOrDefault(),
                NewRowCount = _newRowCount.GetValueOrDefault(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingStreamName = _fundingStreamName ?? NewRandomString(),
                FundingStreamShortName = _fundingStreamShortName,
                ProviderVersionId = _providerVersionId
            };
    }
}