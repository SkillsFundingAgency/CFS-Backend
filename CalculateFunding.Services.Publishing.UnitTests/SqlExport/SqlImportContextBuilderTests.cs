using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.SqlExport;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImportContextBuilderTests : QaSchemaTemplateTest
    {
        private Mock<ICosmosRepository> _cosmos;

        private Mock<ICosmosDbFeedIterator<PublishedProvider>> _publishedProviders;

        private SqlImportContextBuilder _contextBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _cosmos = new Mock<ICosmosRepository>();
            _publishedProviders = new Mock<ICosmosDbFeedIterator<PublishedProvider>>();

            _contextBuilder = new SqlImportContextBuilder(_cosmos.Object,
                Policies.Object,
                TemplateMetadataResolver.Object,
                Specifications.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task CreatesContextWithDocumentFeedForFundingStreamAndPeriodAndInitialisedDataTableBuilders()
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

            SchemaContext schemaContext = new SchemaContext();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));
            FundingTemplateContents fundingTemplate = NewFundingTemplateContents(_ => _.WithSchemaVersion(schemaVersion)
                .WithTemplateFileContents(fundingTemplateContents));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(
                NewFundingLine(fl => fl.WithCalculations(calculationFour,
                        calculationFive)
                    .WithFundingLines(NewFundingLine(fl1 => fl1.WithCalculations(calculationThree)))
                )));
            
            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);
            AndTheCosmosDocumentFeed(specificationId, fundingStreamId);

            ISqlImportContext importContext = await WhenTheImportContextIsBuilt(specificationId, fundingStreamId, schemaContext);

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

        private async Task<ISqlImportContext> WhenTheImportContextIsBuilt(string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext)
            => await _contextBuilder.CreateImportContext(specificationId, fundingStreamId, schemaContext);

        private void AndTheCosmosDocumentFeed(string specificationId,
            string fundingStreamId)
            => _cosmos.Setup(_ => _.GetFeedIterator<PublishedProvider>(It.Is<CosmosDbQuery>(qry
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

        private bool HasParameter(CosmosDbQuery query,
            string key,
            object value)
            => query?.Parameters.Count(_ => _.Name == key &&
                                            _.Value?.Equals(value) == true) == 1;
    }
}