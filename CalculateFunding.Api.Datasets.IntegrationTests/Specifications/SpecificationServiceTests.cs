using CalculateFunding.Api.Datasets.IntegrationTests.Data;
using CalculateFunding.Api.Datasets.IntegrationTests.Datasets;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Specifications
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class SpecificationServiceTests : IntegrationTest
    {
        private static readonly Assembly ResourceAssembly = typeof(SpecificationServiceTests).Assembly;


        private FundingConfigurationDataContext _fundingConfigurationDataContext;
        private SpecificationDataContext _specificationDataContext;

        private IDatasetsApiClient _datasets;

        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddDatasetsInterServiceClient(c),
                AddNullLogger,
                AddUserProvider);
        }

        [TestInitialize]
        public void SetUp()
        {
            _fundingConfigurationDataContext = new FundingConfigurationDataContext(Configuration, ResourceAssembly);
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);

            TrackForTeardown(_fundingConfigurationDataContext,
                _specificationDataContext
                );

            _datasets = GetService<IDatasetsApiClient>();
        }

        [TestMethod]
        [Ignore]
        public async Task ReturnsEligibleSpecificationReferencesWhenGetEligibleSpecificationsToReferenceCalled()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string eligibleSpecificationId = NewRandomString();
            string eligibleFundingPeriodId = NewRandomString();
            string eligibleFundingStreamId = NewRandomString();

            await GivenTheFundingConfiguration(NewFundingConfiguration(_
                => _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithAllowedPublishedFundingStreamsIdsToReference(eligibleFundingStreamId)));

            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(specificationId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId));

            SpecificationTemplateParameters specificationToReference = NewSpecification(_
                => _.WithId(eligibleSpecificationId)
                    .WithFundingPeriodId(eligibleFundingPeriodId)
                    .WithFundingStreamId(eligibleFundingStreamId)
                    .WithIsSelectedForFunding(true));

            await AndTheSpecification(specification);
            await AndTheSpecification(specificationToReference);

            ApiResponse<IEnumerable<EligibleSpecificationReference>> apiResponse 
                = await WhenGetEligibleSpecificationsToReference(specificationId);

            EligibleSpecificationReference eligibleSpecificationReference = apiResponse.Content.SingleOrDefault();

            eligibleSpecificationReference.SpecificationId.Should().Be(eligibleSpecificationId);
            eligibleSpecificationReference.FundingPeriodId.Should().Be(eligibleFundingPeriodId);
            eligibleSpecificationReference.FundingStreamId.Should().Be(eligibleFundingStreamId);
        }

        private async Task<ApiResponse<IEnumerable<EligibleSpecificationReference>>> WhenGetEligibleSpecificationsToReference(string specificationId) =>
            await _datasets.GetEligibleSpecificationsToReference(specificationId);

        private async Task GivenTheFundingConfiguration(FundingConfigurationTemplateParameters parameters)
            => await _fundingConfigurationDataContext.CreateContextData(parameters);

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
        {
            SpecificationTemplateParametersBuilder specificationTemplateParametersBuilder = new SpecificationTemplateParametersBuilder();

            setUp?.Invoke(specificationTemplateParametersBuilder);

            return specificationTemplateParametersBuilder.Build();
        }

        private FundingConfigurationTemplateParameters NewFundingConfiguration(Action<FundingConfigurationTemplateParametersBuilder> setUp = null)
        {
            FundingConfigurationTemplateParametersBuilder fundingConfigurationTemplateParametersBuilder = new FundingConfigurationTemplateParametersBuilder();

            setUp?.Invoke(fundingConfigurationTemplateParametersBuilder);

            return fundingConfigurationTemplateParametersBuilder.Build();
        }

    }
}
