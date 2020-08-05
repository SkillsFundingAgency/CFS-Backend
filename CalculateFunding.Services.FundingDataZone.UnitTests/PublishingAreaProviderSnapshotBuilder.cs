using System;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class PublishingAreaProviderSnapshotBuilder : TestEntityBuilder
    {
        private DateTime? _created;
        private string _description;
        private string _name;
        private int? _version;
        private DateTime? _targetDate;
        private string _fundingStreamCode;
        private string _funidngStreamName;
        private int? _providerSnapshotId;

        public PublishingAreaProviderSnapshotBuilder WithCreated(DateTime created)
        {
            _created = created;

            return this;
        }

        public PublishingAreaProviderSnapshotBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public PublishingAreaProviderSnapshotBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public PublishingAreaProviderSnapshotBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public PublishingAreaProviderSnapshotBuilder WithProviderSnapshotId(int providerSnapshotId)
        {
            _providerSnapshotId = providerSnapshotId;

            return this;
        }

        public PublishingAreaProviderSnapshotBuilder WithTargetDate(DateTime? targetDate)
        {
            _targetDate = targetDate;

            return this;
        }

        public PublishingAreaProviderSnapshotBuilder WithFundingStreamCode(string fundingStreamCode)
        {
            _fundingStreamCode = fundingStreamCode;

            return this;
        }
        
        public PublishingAreaProviderSnapshotBuilder WithFundingStreamName(string fundingStreamName)
        {
            _funidngStreamName = fundingStreamName;

            return this;
        }
        
        public PublishingAreaProviderSnapshot Build()
        {
            return new PublishingAreaProviderSnapshot
            {
                Created = _created.GetValueOrDefault(NewRandomDateTime().DateTime),
                Description = _description ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 100)),
                TargetDate = _targetDate.GetValueOrDefault(NewRandomDateTime().DateTime),
                FundingStreamCode = _fundingStreamCode ?? NewRandomString(),
                FundingStreamName = _funidngStreamName ?? NewRandomString(),
                ProviderSnapshotId = _providerSnapshotId.GetValueOrDefault(NewRandomNumberBetween(1, 9999))
            };
        }
    }
}