using System;
using CalculateFunding.Models.Result.ViewModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests.Validators
{
    public class UpdateFundingStructureLastModifiedRequestBuilder : TestEntityBuilder
    {
        private DateTimeOffset? _lastModified;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _specificationId;

        public UpdateFundingStructureLastModifiedRequestBuilder WithLastModified(DateTimeOffset lastModified)
        {
            _lastModified = lastModified;

            return this;
        }
        
        public UpdateFundingStructureLastModifiedRequestBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }
        
        public UpdateFundingStructureLastModifiedRequestBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }
        
        public UpdateFundingStructureLastModifiedRequestBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }
        
        public UpdateFundingStructureLastModifiedRequest Build()
        {
            return new UpdateFundingStructureLastModifiedRequest
            {
                SpecificationId = _specificationId,
                FundingPeriodId = _fundingPeriodId,
                FundingStreamId = _fundingStreamId,
                LastModified = _lastModified.GetValueOrDefault()
            };
        }
    }
}