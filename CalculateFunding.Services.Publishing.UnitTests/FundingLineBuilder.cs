using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private FundingLineType? _fundingLineType;
        private uint? _templateLineId;
        private decimal? _value;
        private IEnumerable<DistributionPeriod> _distributionPeriods;
        private string _fundingLineCode;
        private string _name;

        public FundingLineBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FundingLineBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public FundingLineBuilder WithValue(decimal? value)
        {
            _value = value;

            return this;
        }

        public FundingLineBuilder WithTemplateLineId(uint templateLineId)
        {
            _templateLineId = templateLineId;

            return this;
        }

        public FundingLineBuilder WithFundingLineType(FundingLineType fundingLineType)
        {
            _fundingLineType = fundingLineType;

            return this;
        }

        public FundingLineBuilder WithDistributionPeriods(params DistributionPeriod[] distributionPeriods)
        {
            _distributionPeriods = distributionPeriods;

            return this;
        }

        public FundingLine Build()
        {
            return new FundingLine
            {
                TemplateLineId = _templateLineId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue)),
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Type = _fundingLineType.GetValueOrDefault(NewRandomEnum<FundingLineType>()),
                Value = _value,
                DistributionPeriods = _distributionPeriods
            };
        }
    }
}