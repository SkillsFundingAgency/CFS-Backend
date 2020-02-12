using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.Transforms
{
    [Binding]
    public class FundingConfigurationTransforms
    {
        [StepArgumentTransformation]
        public IEnumerable<FundingVariation> TransformFundingVariations(Table fundingVariationTable)
        {
            fundingVariationTable
                .Should()
                .NotBeNull();
            fundingVariationTable
                .RowCount
                .Should()
                .BeGreaterThan(0);

            return fundingVariationTable.CreateSet<FundingVariation>();
        }
    }
}