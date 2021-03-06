using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing.SqlExport;
using CalculateFunding.Services.Publishing.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class QaSchemaServiceTests : QaSchemaTemplateTest
    {
        private Mock<ISqlNameGenerator> _sqlNameGenerator;
        private Mock<ISqlSchemaGenerator> _sqlSchemaGenerator;
        private Mock<IQaRepository> _qa;
        private Mock<IProfilingApiClient> _profiling;
        
        private QaSchemaService _schemaService;

        [TestInitialize]
        public void SetUp()
        {
            _sqlNameGenerator = new Mock<ISqlNameGenerator>();
            _sqlSchemaGenerator = new Mock<ISqlSchemaGenerator>();
            _profiling = new Mock<IProfilingApiClient>();
            _qa = new Mock<IQaRepository>();

            _sqlNameGenerator.Setup(_ => _.GenerateIdentifier(It.IsAny<string>()))
                .Returns<string>(input => input);
            
            _schemaService = new QaSchemaService(Policies.Object,
                Specifications.Object,
                TemplateMetadataResolver.Object,
                _sqlSchemaGenerator.Object,
                _qa.Object,
                _profiling.Object,
                _sqlNameGenerator.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task CreatesDDLMatchingSpecificationFundingAndReCreatesSchemaObjectsInQaRepository()
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

            string fundingLineOneCode = NewRandomString();
            string fundingLineTwoCode = NewRandomString();
            
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingStreamId, templateVersion)));
            FundingTemplateContents fundingTemplate = NewFundingTemplateContents(_ => _.WithSchemaVersion(schemaVersion)
                .WithTemplateFileContents(fundingTemplateContents));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(
                NewFundingLine(fl => fl.WithCalculations(calculationFour,
                        calculationFive)
                    .WithFundingLineCode(fundingLineOneCode)
                    .WithFundingLines(NewFundingLine(fl1 => fl1.WithCalculations(calculationThree)
                        .WithFundingLineCode(fundingLineTwoCode)))
                )));

            FundingStreamPeriodProfilePattern profilePatternOne = NewFundingStreamPeriodProfilePattern(_ => 
                _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingLineId(fundingLineOneCode));
            FundingStreamPeriodProfilePattern profilePatternTwo = NewFundingStreamPeriodProfilePattern(_ => 
                _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingLineId(fundingLineTwoCode));
            
            GivenTheSpecification(specificationId, specificationSummary);
            AndTheFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion, fundingTemplate);
            AndTheTemplateMetadataContents(schemaVersion, fundingTemplateContents, templateMetadataContents);
            AndTheProfiling(fundingStreamId, fundingPeriodId, profilePatternOne, profilePatternTwo);
            
            await WhenTheSchemaIsRecreated(specificationId, fundingStreamId);
               
            ThenTheTotalNumberOfDDLScriptsExecutedWas(32);
        }

        private void ThenTheTotalNumberOfDDLScriptsExecutedWas(int count)
        {
            _qa.Verify(_ => _.ExecuteSql(It.IsAny<string>()),
                Times.Exactly(count));
        }

        private void AndTheProfiling(string fundingStreamId,
            string fundingPeriodId,
            params FundingStreamPeriodProfilePattern[] periodPatterns)
            => _profiling.Setup(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>(HttpStatusCode.OK, periodPatterns));

        private static bool ColumnsMatch(IEnumerable<SqlColumnDefinition> actualColumns,
            IEnumerable<SqlColumnDefinition> expectedColumns)
        {
            return expectedColumns.Count() == actualColumns.Count() &&
                   expectedColumns
                .All(_ => actualColumns.Count(col =>
                    col.Name == _.Name &&
                    col.Type == _.Type &&
                    col.AllowNulls == _.AllowNulls) == 1);
        }

        private async Task WhenTheSchemaIsRecreated(string specificationId,
            string fundingStreamId)
            => await _schemaService.ReCreateTablesForSpecificationAndFundingStream(specificationId, fundingStreamId);

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);
            
            return fundingStreamPeriodProfilePatternBuilder.Build();
        }
    }
}