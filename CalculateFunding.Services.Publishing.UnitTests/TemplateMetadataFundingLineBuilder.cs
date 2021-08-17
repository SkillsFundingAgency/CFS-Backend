using CalculateFunding.Tests.Common.Helpers;
using TemplateMetadataFundingLine = CalculateFunding.Common.ApiClient.Policies.Models.TemplateMetadataFundingLine;
using FundingLineType = CalculateFunding.Common.TemplateMetadata.Enums.FundingLineType;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class TemplateMetadataFundingLineBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private string _name;
        private uint? _templateLineId;
        private FundingLineType _fundingLineType;

        public TemplateMetadataFundingLineBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public TemplateMetadataFundingLineBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public TemplateMetadataFundingLineBuilder WithTemplateLineId(uint templateLineId)
        {
            _templateLineId = templateLineId;

            return this;
        }

        public TemplateMetadataFundingLineBuilder WithFundingLineType(FundingLineType fundingLineType)
        {
            _fundingLineType = fundingLineType;

            return this;
        }

        public TemplateMetadataFundingLine Build()
        {
            return new TemplateMetadataFundingLine
            {
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                TemplateLineId = _templateLineId ?? NewRandomUint(),
                Type = _fundingLineType
            };
        }
    }
}
