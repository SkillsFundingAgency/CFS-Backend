using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class SpecificationTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _specificationVersionId;
        private string _name;
        private string _fundingPeriodId;
        private string _fundingPeriodName;
        private string _providerVersionId;
        private Reference[] _fundingStreams;
        private string _description;
        private string[] _dataDefinitionRelationshipIds;
        private IDictionary<string, string> _templateIds;
        private ProviderSource? _providerSource;
        private int? _providerSnapshotId;
        private int _version;
        private string _authorId;
        private string _authorName;
        private PublishStatus? _publishStatus;

        public SpecificationTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithSpecificationVersionId(string specificationVersionId)
        {
            _specificationVersionId = specificationVersionId;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithFundingPeriodName(string fundingPeriodName)
        {
            _fundingPeriodName = fundingPeriodName;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithProviderVersionId(string providerVersionId)
        {
            _providerVersionId = providerVersionId;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreams = new[]
            {
                new Reference
                {
                    Id = fundingStreamId,
                    Name = NewRandomString()
                }
            };

            return this;
        }

        public SpecificationTemplateParametersBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithDataDefinitionRelationshipIds(params string[] dataDefinitionRelationshipsIds)
        {
            _dataDefinitionRelationshipIds = dataDefinitionRelationshipsIds;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithTemplateIds(params (string fundingStream, string templateId)[] templateIds)
        {
            _templateIds = templateIds.ToDictionary(_ => _.fundingStream, _ => _.templateId);

            return this;
        }

        public SpecificationTemplateParametersBuilder WithProviderSource(ProviderSource providerSource)
        {
            _providerSource = providerSource;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithProviderSnapshotId(int providerSnapShotId)
        {
            _providerSnapshotId = providerSnapShotId;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithAuthorId(string authorId)
        {
            _authorId = authorId;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithAuthorName(string authorName)
        {
            _authorName = authorName;

            return this;
        }

        public SpecificationTemplateParametersBuilder WithPublishStatus(PublishStatus publishStatus)
        {
            _publishStatus = publishStatus;

            return this;
        }

        public SpecificationTemplateParameters Build() =>
            new SpecificationTemplateParameters
            {
                Id = _id ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Version = _version,
                AuthorId = _authorId ?? NewRandomString(),
                AuthorName = _authorName ?? NewRandomString(),
                FundingStreams = _fundingStreams,
                ProviderSource = _providerSource.GetValueOrDefault(NewRandomEnum<ProviderSource>()),
                PublishStatus = _publishStatus.GetValueOrDefault(NewRandomEnum<PublishStatus>()),
                TemplateIds = _templateIds,
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingPeriodName = _fundingPeriodName ?? NewRandomString(),
                ProviderSnapshotId = _providerSnapshotId,
                ProviderVersionId = _providerVersionId ?? NewRandomString(),
                SpecificationVersionId = _specificationVersionId ?? NewRandomString(),
                DataDefinitionRelationshipIds = _dataDefinitionRelationshipIds ?? new string[0]
            };
    }
}