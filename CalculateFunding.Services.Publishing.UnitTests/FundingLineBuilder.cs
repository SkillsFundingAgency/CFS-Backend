using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class FundingLineBuilder : TestEntityBuilder
    {
        private OrganisationGroupingReason? _organisationGroupingReason;
        private uint? _templateLineId;
        private decimal _value;

        public FundingLineBuilder WithValue(decimal value)
        {
            _value = value;

            return this;
        }

        public FundingLineBuilder WithTemplateLineId(uint templateLineId)
        {
            _templateLineId = templateLineId;

            return this;
        }
        
        public FundingLineBuilder WithOrganisationGroupingReason(OrganisationGroupingReason organisationGroupingReason)
        {
            _organisationGroupingReason = organisationGroupingReason;

            return this;
        }
        
        public FundingLine Build()
        {
            return new FundingLine
            {
                TemplateLineId = _templateLineId.GetValueOrDefault((uint)NewRandomNumberBetween(1, int.MaxValue)),
                Type = _organisationGroupingReason.GetValueOrDefault(NewRandomEnum<OrganisationGroupingReason>()),
                Value = _value
            };
        }
    }
}