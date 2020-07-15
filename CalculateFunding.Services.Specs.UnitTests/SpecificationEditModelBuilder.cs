using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Specs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class SpecificationEditModelBuilder : TestEntityBuilder
    {
        private IDictionary<string, string> _assignedTemplateIds;
        private string _specificationId;
        private string _fundingPeriodId;

        public SpecificationEditModelBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }

        public SpecificationEditModelBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }
        
        public SpecificationEditModelBuilder WithAssignedTemplateIds(params (string fundingStreamId, string templateId)[] assignedTemplateIds)
        {
            _assignedTemplateIds = assignedTemplateIds.ToDictionary(_ => _.fundingStreamId, _ => _.templateId);

            return this;
        }
        
        public SpecificationEditModel Build()
        {
            return new SpecificationEditModel
            {
                SpecificationId = _specificationId ?? NewRandomString(),
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                AssignedTemplateIds = _assignedTemplateIds
            };
        }
    }
}