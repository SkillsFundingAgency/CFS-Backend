using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;
using Enum = CalculateFunding.Models.Graph.Enum;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class EnumBuilder
    {
        private string _enumName;
        private string _enumValue;
        private string _fundingStream;
        private string _specificaionId;

        public EnumBuilder WithEnumName(string enumName)
        {
            _enumName = enumName;

            return this;
        }

        public EnumBuilder WithEnumValue(string enumValue)
        {
            _enumValue = enumValue;

            return this;
        }

        public EnumBuilder WithFundingStreamId(string fundingStream)
        {
            _fundingStream = fundingStream;
            return this;
        }

        public EnumBuilder WithSpecificationId(string specificaionId)
        {
            _specificaionId = specificaionId;
            return this;
        }

        public Enum Build()
        {
            return new Enum
            {
                EnumName = _enumName ?? new RandomString(),
                EnumValue = _enumValue ?? new RandomString(),
                FundingStreamId = _fundingStream ?? new RandomString(),
                SpecificationId = _specificaionId ?? new RandomString(),
            };
        }
    }
}
