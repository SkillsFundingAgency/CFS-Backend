using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.SqlExport;
using FluentAssertions;
using Microsoft.FeatureManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImportContextBuilderTests : QaSchemaTemplateTest
    {
        private Mock<ICosmosRepository> _cosmos;

        private Mock<ICosmosDbFeedIterator> _publishedProviders;

        private Mock<ICosmosDbFeedIterator> _publishedProviderVersions;

        private Mock<IReleaseManagementRepository> _releaseManagementRepository;

        private Mock<IFeatureManagerSnapshot> _featureManagerSnapshot;

        private SqlImportContextBuilder _contextBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _cosmos = new Mock<ICosmosRepository>();
            _publishedProviders = new Mock<ICosmosDbFeedIterator>();
            _publishedProviderVersions = new Mock<ICosmosDbFeedIterator>();
            _featureManagerSnapshot = new Mock<IFeatureManagerSnapshot>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();

            _contextBuilder = new SqlImportContextBuilder(_cosmos.Object,
                Policies.Object,
                TemplateMetadataResolver.Object,
                Specifications.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                _releaseManagementRepository.Object,
                _featureManagerSnapshot.Object);
        }

        [DataTestMethod]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion)]
        public async Task CreatesContextWithDocumentFeedForFundingStreamAndPeriodAndInitialisedDataTableBuilders(
            SqlExportSource sqlExportSource)
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            string schemaVersion = NewRandomString();
            string fundingTemplateContents = NewRandomString();

            Calculation calculationOne = NewCalculation();
            Calculation calculationTwo = NewCalculation();
            Calculation calculationThree = NewCalculation();
            Calculation calculationFour = NewCalculation(_ => _.WithCalculations(calculationOne));
            Calculation calculationFive = NewCalculation(_ => _.WithCalculations(calculationTwo));

            SchemaContext schemaContext = new();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));

            FundingConfigurationChannel fundingConfigurationChannel 
                = NewFundingConfigurationChannel(_ => _.WithChannelCode("ch1"));
            FundingConfiguration fundingConfiguration 
                = NewFundingConfiguration(_ => _.WithReleaseChannels(fundingConfigurationChannel));
            IEnumerable<Channel> channels = new[] { NewChannel(_ => _.WithChannelCode("ch1").WithChannelId(1)) };
            FundingTemplateContents fundingTemplate = NewFundingTemplateContents(_ => _.WithSchemaVersion(schemaVersion)
                .WithTemplateFileContents(fundingTemplateContents));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(
                NewFundingLine(fl => fl.WithCalculations(calculationFour,
                        calculationFive)
                    .WithFundingLines(NewFundingLine(fl1 => fl1.WithCalculations(calculationThree)))
                )));
            
            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);
            AndTheChannels(channels);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);
            AndThePublishedProviderCosmosDocumentFeed(specificationId, fundingStreamId);


            AndTheReleasedPublishedProviderVersionFeed(fundingPeriodId, fundingStreamId);


            ISqlImportContext importContext = await WhenTheImportContextIsBuilt(specificationId, fundingStreamId, schemaContext, sqlExportSource);

            importContext
                .Should()
                .BeOfType<SqlImportContext>();

            importContext
                .SchemaContext
                .Should()
                .BeSameAs(schemaContext);

            importContext
                .Calculations
                .Should()
                .BeOfType<CalculationDataTableBuilder>();

            importContext
                .PaymentFundingLines
                .Should()
                .BeOfType<PaymentFundingLineDataTableBuilder>();
            
            importContext
                .InformationFundingLines
                .Should()
                .BeOfType<InformationFundingLineDataTableBuilder>();

            //profiling is lazily initialised from the first dto profile periods
            importContext
                .Profiling
                .Should()
                .BeNull();
            
            importContext
                .Calculations
                .Should()
                .BeOfType<CalculationDataTableBuilder>();

            importContext
                .Providers
                .Should()
                .BeOfType<ProviderDataTableBuilder>();

            importContext
                .Funding
                .Should()
                .BeOfType<PublishedProviderVersionDataTableBuilder>();
            
            AndTheImportContextHasADataTableBuilderForTheCalculation(importContext, calculationOne);
            AndTheImportContextHasADataTableBuilderForTheCalculation(importContext, calculationTwo);
            AndTheImportContextHasADataTableBuilderForTheCalculation(importContext, calculationThree);
            AndTheImportContextHasADataTableBuilderForTheCalculation(importContext, calculationFour);
            AndTheImportContextHasADataTableBuilderForTheCalculation(importContext, calculationFive);
        }

        private void AndTheImportContextHasADataTableBuilderForTheCalculation(ISqlImportContext importContext,
            Calculation calculation)
        {
            importContext.CalculationNames
                .TryGetValue(calculation.TemplateCalculationId, out string calculationName)
                .Should()
                .BeTrue();

            calculationName
                .Should()
                .Be(calculation.Name);
        }

        protected void AndTheChannels(
            IEnumerable<Channel> channels)
            => _releaseManagementRepository.Setup(_ => _.GetChannels()).ReturnsAsync(channels);

        private async Task<ISqlImportContext> WhenTheImportContextIsBuilt(string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext,
            SqlExportSource sqlExportSource)
            => await _contextBuilder.CreateImportContext(specificationId, fundingStreamId, schemaContext, sqlExportSource);

        private void AndThePublishedProviderCosmosDocumentFeed(string specificationId,
            string fundingStreamId)
            => _cosmos.Setup(_ => _.GetFeedIterator(It.Is<CosmosDbQuery>(qry
                        => qry.QueryText == @"SELECT
                              *
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.specificationId = @specificationId
                        AND p.deleted = false" &&
                           HasParameter(qry, "@specificationId", specificationId) &&
                           HasParameter(qry, "@fundingStreamId", fundingStreamId)),
                    100, null))
                .Returns(_publishedProviders.Object);

        private void AndTheReleasedPublishedProviderVersionFeed(string fundingPeriodId,
            string fundingStreamId)
            => _cosmos.Setup(_ => _.GetFeedIterator(It.Is<CosmosDbQuery>(qry
                => qry.QueryText == @"SELECT
                                  *
                            FROM publishedProvider p
                            WHERE p.documentType = 'PublishedProviderVersion'
                            AND p.deleted = false                            
                            AND p.content.status = 'Released'
                            AND p.content.fundingStreamId = @fundingStreamId
                            AND p.content.fundingPeriodId = @fundingPeriodId" &&
                   HasParameter(qry, "@fundingPeriodId", fundingPeriodId) &&
                   HasParameter(qry, "@fundingStreamId", fundingStreamId)),
            100, null))
        .Returns(_publishedProviderVersions.Object);

        private static bool HasParameter(CosmosDbQuery query,
            string key,
            object value)
            => query?.Parameters.Count(_ => _.Name == key &&
                                            _.Value?.Equals(value) == true) == 1;
    }
}