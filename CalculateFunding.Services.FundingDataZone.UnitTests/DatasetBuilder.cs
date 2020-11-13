using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class DatasetBuilder : TestEntityBuilder
    {
        private DateTime? _createdDate;
        private string _datasetCode;
        private string _description;
        private string _displayName;
        private string _fundingStreamId;
        private GroupingLevel? _groupingLevel;
        private string _identifierColumnName;
        private IdentifierType? _identifierType;
        private string _originatingSystem;
        private string _originatingSystemVersion;
        private string _providerSnapshotId;
        private string _tableName;
        private int? _version;
        private Dictionary<string, string> _properties;

        public DatasetBuilder WithCreatedDate(DateTime createdDate)
        {
            _createdDate = createdDate;

            return this;
        }

        public DatasetBuilder WithDatasetCode(string datasetCode)
        {
            _datasetCode = datasetCode;

            return this;
        }

        public DatasetBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public DatasetBuilder WithDisplayName(string displayName)
        {
            _displayName = displayName;

            return this;
        }

        public DatasetBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }

        public DatasetBuilder WithGroupingLevel(GroupingLevel groupingLevel)
        {
            _groupingLevel = groupingLevel;

            return this;
        }

        public DatasetBuilder WithIdentifierColumnName(string identifierColumnName)
        {
            _identifierColumnName = identifierColumnName;

            return this;
        }

        public DatasetBuilder WithOriginatingSystem(string originatingSystem)
        {
            _originatingSystem = originatingSystem;

            return this;
        }

        public DatasetBuilder WithOriginatingSystemVersion(string originatingSystemVersion)
        {
            _originatingSystemVersion = originatingSystemVersion;

            return this;
        }

        public DatasetBuilder WithProviderSnapshotId(string providerSnapshotId)
        {
            _providerSnapshotId = providerSnapshotId;

            return this;
        }

        public DatasetBuilder TableName(string tableName)
        {
            _tableName = tableName;

            return this;
        }

        public DatasetBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public DatasetBuilder WithProperties(params (string name, string value)[] properties)
        {
            _properties = properties.ToDictionary(_ => _.name, _ => _.value);

            return this;
        }

        public DatasetBuilder WithIdentifierType(IdentifierType identifierType)
        {
            _identifierType = identifierType;

            return this;
        }
        
        public Dataset Build()
        {
            return new Dataset
            {
                Description = _description ?? NewRandomString(),
                Properties = _properties ?? new Dictionary<string, string>
                {
                    {NewRandomString(), NewRandomString()},
                    {NewRandomString(), NewRandomString()},
                },
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99)),
                CreatedDate = _createdDate.GetValueOrDefault(NewRandomDateTime().DateTime),
                DatasetCode = _datasetCode ?? NewRandomString(),
                DisplayName = _displayName ?? NewRandomString(),
                GroupingLevel = _groupingLevel.GetValueOrDefault(NewRandomEnum<GroupingLevel>()),
                IdentifierType = _identifierType.GetValueOrDefault(NewRandomEnum<IdentifierType>()),
                OriginatingSystem = _originatingSystem ?? NewRandomString(),
                TableName = _tableName ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                IdentifierColumnName = _identifierColumnName ?? NewRandomString(),
                OriginatingSystemVersion = _originatingSystemVersion ?? NewRandomString(),
                ProviderSnapshotId = _providerSnapshotId ?? NewRandomString()
            };
        }
    }
}