using System;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class ProviderSnapshotBuilder : TestEntityBuilder
    {
        private int? _id;
        private string _name;
        private string _description;
        private int? _version;
        private DateTime? _targetDate;
        private DateTime? _created;
        private string _fundingStreamCode;
        private string _fundingStreamName;

        public ProviderSnapshotBuilder WithId(int id)
        {
            _id = id;

            return this;
        }
        
        public ProviderSnapshotBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ProviderSnapshotBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public ProviderSnapshotBuilder WithVersion(int version)
        {
            _version = version;

            return this;
        }

        public ProviderSnapshotBuilder WithTargetDate(DateTime targetDate)
        {
            _targetDate = targetDate;

            return this;
        }
        
        public ProviderSnapshotBuilder WithCreated(DateTime created)
        {
            _created = created;

            return this;
        }
        
        public ProviderSnapshotBuilder WithFundingStreamCode(string fundingStreamCode)
        {
            _fundingStreamCode = fundingStreamCode;

            return this;
        }

        public ProviderSnapshotBuilder WithFundingStreamName(string fundingStreamName)
        {
            _fundingStreamName = fundingStreamName;

            return this;
        }
        
        public ProviderSnapshot Build()
        {
            return new ProviderSnapshot
            {
                ProviderSnapshotId = _id.GetValueOrDefault(NewRandomNumberBetween(1, int.MaxValue)),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                Version = _version.GetValueOrDefault(NewRandomNumberBetween(1, 99)),
                TargetDate = _targetDate.GetValueOrDefault(NewRandomDateTime().DateTime),
                Created = _created.GetValueOrDefault(NewRandomDateTime().DateTime),
                FundingStreamCode = _fundingStreamCode ?? NewRandomString(),
                FundingStreamName = _fundingStreamName ?? NewRandomString()
            };
        }
    }
}