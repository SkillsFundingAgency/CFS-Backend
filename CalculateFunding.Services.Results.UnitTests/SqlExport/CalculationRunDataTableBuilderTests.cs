using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.SqlExport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class CalculationRunDataTableBuilderTests : DataTableBuilderTest<CalculationRunDataTableBuilder>
    {
        private string specificationName;
        private string templateVersion;
        private string providerVersion;
        private DateTime lastUpdated;
        private string lastUpdatedBy;


        [TestInitialize]
        public void SetUp()
        {
            FundingStreamId = NewRandomStringWithMaxLength(32);
            FundingPeriodId = NewRandomStringWithMaxLength(32);
            SpecificationId = NewRandomStringWithMaxLength(32);

            specificationName = NewRandomString();
            templateVersion = NewRandomString();
            providerVersion = NewRandomString();
            lastUpdated = DateTime.UtcNow;
            lastUpdatedBy = NewRandomString();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithFundingStreamIds(FundingStreamId)
                .WithFundingPeriodId(FundingPeriodId)
                .WithName(specificationName)
                .WithTemplateIds((FundingStreamId, templateVersion))
                .WithProviderVersionId(providerVersion));
            JobSummary jobSummary = NewJobSummary(_ => _.WithLastUpdated(lastUpdated).WithInvokerUserDisplayName(lastUpdatedBy));

            DataTableBuilder = new CalculationRunDataTableBuilder(specificationSummary, jobSummary);
        }

        [TestMethod]
        public void MapsTemplateCalculationsIntoDataTable()
        {
            ProviderResult rowOne = NewProviderResult(_ => _.WithSpecificationId(SpecificationId));
            ProviderResult rowTwo = NewProviderResult(_ => _.WithSpecificationId(SpecificationId));

            WhenTheRowsAreAdded(rowOne, rowTwo);

            ThenTheDataTableHasColumnsMatching(
                NewDataColumn<string>("SpecificationId"),
                NewDataColumn<string>("FundingStreamId"),
                NewDataColumn<string>("FundingPeriodId"),
                NewDataColumn<string>("SpecificationName"),
                NewDataColumn<string>("TemplateVersion"),
                NewDataColumn<string>("ProviderVersion"),
                NewDataColumn<DateTime>("LastUpdated"),
                NewDataColumn<string>("LastUpdatedBy"));
            AndTheDataTableHasRowsMatching(
                NewRow(
                    rowOne.SpecificationId,
                    FundingStreamId,
                    FundingPeriodId,
                    specificationName,
                    templateVersion,
                    providerVersion,
                    lastUpdated,
                    lastUpdatedBy));
            AndTheTableNameIs($"[dbo].[{SpecificationId}_CalculationRun]");
        }
    }
}
