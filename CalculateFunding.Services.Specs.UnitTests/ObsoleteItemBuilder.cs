using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class ObsoleteItemBuilder : TestEntityBuilder
    {
        private uint? _fundingLineId;
        private string _specificationId;
        private string _enumValueName;
        private string[] _calculationIds = new string[0];
        private uint? _templateCalculationId;
        private ObsoleteItemType? _itemType;
        private string _codeReference;
        private string _fundingLineName;

        public ObsoleteItemBuilder WithCodeReference(string sourceCode)
        {
            _codeReference = sourceCode;
            return this;
        }

        public ObsoleteItemBuilder WithFundingLineName(string fundingLineName)
        {
            _fundingLineName = fundingLineName;
            return this;
        }

        public ObsoleteItemBuilder WithFundingLineId(uint fundingLineId)
        {
            _fundingLineId = fundingLineId;
            return this;
        }

        public ObsoleteItemBuilder WithCalculationIds(params string[] calculationIds)
        {
            _calculationIds = calculationIds;
            return this;
        }

        public ObsoleteItemBuilder WithEnumValueName(string enumValueName)
        {
            _enumValueName = enumValueName;
            return this;
        }

        public ObsoleteItemBuilder WithTemplateCalculationId(uint templateCalculationId)
        {
            _templateCalculationId = templateCalculationId;
            return this;
        }

        public ObsoleteItemBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;
            return this;
        }

        public ObsoleteItemBuilder WithItemType(ObsoleteItemType? itemType)
        {
            _itemType = itemType;
            return this;
        }

        public ObsoleteItem Build()
        {
            return new ObsoleteItem()
            { 
                FundingLineId = _fundingLineId,
                CalculationIds = _calculationIds,
                SpecificationId = _specificationId ?? NewRandomString(),
                TemplateCalculationId = _templateCalculationId.GetValueOrDefault(NewRandomUint()),
                EnumValueName = _enumValueName,
                ItemType = _itemType.GetValueOrDefault(NewRandomEnum<ObsoleteItemType>()),
                CodeReference = _codeReference,
                FundingLineName = _fundingLineName
            };
        }
    }
}