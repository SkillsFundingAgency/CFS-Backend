using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalcsApiCalculationIdentifier = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationIdentifier;
using CalcsApiGenerateIdentifierModel = CalculateFunding.Common.ApiClient.Calcs.Models.GenerateIdentifierModel;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImportContextBuilderTests : QaSchemaTemplateTest
    {
        private Mock<ICosmosRepository> _cosmos;
        private Mock<ICosmosDbFeedIterator> _publishedProviders;
        private Mock<ISqlNameGenerator> _sqlNameGenerator;

        private SqlImportContextBuilder _contextBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _cosmos = new Mock<ICosmosRepository>();
            _publishedProviders = new Mock<ICosmosDbFeedIterator>();
            _sqlNameGenerator = new Mock<ISqlNameGenerator>();

            _contextBuilder = new SqlImportContextBuilder(_cosmos.Object,
                Policies.Object,
                TemplateMetadataResolver.Object,
                Specifications.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync(),
                    CalculationsApiClient = Policy.NoOpAsync(),
                    JobsApiClient = Policy.NoOpAsync()
                },
                _sqlNameGenerator.Object,
                Calculations.Object,
                Jobs.Object);
        }

        [TestMethod]
        public async Task CreatesContextWithDocumentFeedForFundingStreamAndPeriodAndInitialisedDataTableBuilders()
        {
            string specificationId = NewRandomString();
            string specificationName = NewRandomString();
            string specificationGeneratedIdentifierName = NewRandomString();
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

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithName(specificationName)
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

            IEnumerable<CalcsApiCalculation> calculations = new List<CalcsApiCalculation>()
            {
                NewApiCalculation(_ => _.WithType(Common.ApiClient.Calcs.Models.CalculationType.Template)),
                NewApiCalculation(_ => _.WithType(Common.ApiClient.Calcs.Models.CalculationType.Additional))
            };

            CalcsApiCalculationIdentifier calcsApiCalculationIdentifier = new CalcsApiCalculationIdentifier
            {
                Name = specificationName,
                SourceCodeName = specificationGeneratedIdentifierName
            };

            JobSummary jobSummary = NewJobSummary();

            AndTheCalculationsForSpecification(specificationId, calculations);
            AndTheGenerateCalculationIdentifier(specificationName, calcsApiCalculationIdentifier);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);
            AndTheCosmosDocumentFeed(specificationId, fundingStreamId);
            AndTheGetLatestSuccessfulJobForSpecification(specificationId, JobConstants.DefinitionNames.CreateInstructAllocationJob, jobSummary);

            ISqlImportContext importContext = await WhenTheImportContextIsBuilt(specificationId);

            importContext
                .Should()
                .BeOfType<SqlImportContext>();

            importContext
                .CalculationRuns
                .Should()
                .BeOfType<CalculationRunDataTableBuilder>();

            importContext
                .PaymentFundingLines
                .Should()
                .BeOfType<PaymentFundingLineDataTableBuilder>();

            importContext
                .InformationFundingLines
                .Should()
                .BeOfType<InformationFundingLineDataTableBuilder>();

            importContext
                .ProviderSummaries
                .Should()
                .BeOfType<ProviderSummaryDataTableBuilder>();

            importContext
                .TemplateCalculations
                .Should()
                .BeOfType<TemplateCalculationsDataTableBuilder>();

            importContext
                .AdditionalCalculations
                .Should()
                .BeOfType<AdditionalCalculationsDataTableBuilder>();

            importContext
                .Providers
                .Should()
                .BeOfType<HashSet<string>>();
        }

        private async Task<ISqlImportContext> WhenTheImportContextIsBuilt(string specificationId)
            => await _contextBuilder.CreateImportContext(specificationId, new HashSet<string>());

        private void AndTheCosmosDocumentFeed(string specificationId,
            string fundingStreamId)
            => _cosmos.Setup(_ => _.GetFeedIterator(It.Is<CosmosDbQuery>(qry
                        => qry.QueryText == @"SELECT
                              *
                        FROM publishedProvider p
                        WHERE p.documentType = 'ProviderResult'
                        AND p.content.current.specificationId = @specificationId
                        AND p.deleted = false" &&
                           HasParameter(qry, "@specificationId", specificationId)),
                    100, null))
                .Returns(_publishedProviders.Object);

        private bool HasParameter(CosmosDbQuery query,
            string key,
            object value)
            => query?.Parameters.Count(_ => _.Name == key &&
                                            _.Value?.Equals(value) == true) == 1;
    }
}
