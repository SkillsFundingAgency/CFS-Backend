using System;
using System.Data;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using TemplateMetadataCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    public abstract class DataTableBuilderTest<TBuilder>
        where TBuilder : IDataTableBuilder<ProviderResult>
    {
        protected TBuilder DataTableBuilder;
        protected DataTable ExpectedDataTable;

        protected string FundingStreamId;
        protected string FundingPeriodId;
        protected string SpecificationId;
        protected string SpecificationName;
        protected string SpecificationIdentifierName;

        [TestInitialize]
        public void DataTableBuilderTestSetUp()
        {
            ExpectedDataTable = new DataTable();

            SpecificationId = NewRandomStringWithMaxLength(32);
            SpecificationName = NewRandomStringWithMaxLength(32);
            SpecificationIdentifierName = NewRandomStringWithMaxLength(32);
        }

        protected T[] AsArray<T>(params T[] items) => items;

        protected void WhenTheRowsAreAdded(params ProviderResult[] rows)
            => DataTableBuilder.AddRows(rows);

        protected Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        protected ProviderResult NewProviderResult(Action<ProviderResultBuilder> setUp = null)
        {
            ProviderResultBuilder providerResultBuilder = new ProviderResultBuilder();

            setUp?.Invoke(providerResultBuilder);

            return providerResultBuilder.Build();
        }

        protected FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        protected FundingLineResult NewFundingLineResult(Action<FundingLineResultBuilder> setUp = null)
        {
            FundingLineResultBuilder fundingLineResultBuilder = new FundingLineResultBuilder();

            setUp?.Invoke(fundingLineResultBuilder);

            return fundingLineResultBuilder.Build();
        }

        protected CalculationResult NewCalculationResult(Action<CalculationResultBuilder> setUp = null)
        {
            CalculationResultBuilder calculationResultBuilder = new CalculationResultBuilder();

            setUp?.Invoke(calculationResultBuilder);

            return calculationResultBuilder.Build();
        }

        protected ProviderSummary NewProviderSummary(Action<ProviderSummaryBuilder> setUp = null)
        {
            ProviderSummaryBuilder providerSummaryBuilder = new ProviderSummaryBuilder();

            setUp?.Invoke(providerSummaryBuilder);

            return providerSummaryBuilder.Build();
        }

        protected CalcsApiCalculation NewApiCalculation(Action<ApiCalculationBuilder> setUp = null)
        {
            ApiCalculationBuilder calculationBuilder = new ApiCalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        protected SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected JobSummary NewJobSummary(Action<JobSummaryBuilder> setUp = null)
        {
            JobSummaryBuilder jobSummaryBuilder = new JobSummaryBuilder();

            setUp?.Invoke(jobSummaryBuilder);

            return jobSummaryBuilder.Build();
        }

        protected TemplateMetadataCalculation NewTemplateMetadataCalculation(Action<TemplateMetadataCalculationBuilder> setUp = null)
        {
            TemplateMetadataCalculationBuilder templateMetadataCalculationBuilder = new TemplateMetadataCalculationBuilder();

            setUp?.Invoke(templateMetadataCalculationBuilder);

            return templateMetadataCalculationBuilder.Build();
        }

        protected DataColumn NewDataColumn<T>(string name,
            int? maxLength = null,
            bool allowNull = false)
        {
            DataColumn column = new DataColumn(name, typeof(T))
            {
                AllowDBNull = allowNull,
            };

            if (maxLength.HasValue)
            {
                column.MaxLength = maxLength.GetValueOrDefault();
            }

            return column;
        }

        protected void ThenTheDataTableHasColumnsMatching(params DataColumn[] expectedColumns)
        {
            ExpectedDataTable.Columns.AddRange(expectedColumns);
        }

        protected void AndTheDataTableHasRowsMatching(params object[][] expectedRows)
        {
            foreach (object[] row in expectedRows)
            {
                ExpectedDataTable.Rows.Add(row);
            }

            int rowNumber = 0;
            foreach (DataRow row in DataTableBuilder.DataTable.Rows)
            {
                int colNumber = 0;
                foreach (object item in row.ItemArray)
                {
                    if (!(item is DBNull))
                    {
                        ExpectedDataTable.Rows[rowNumber][colNumber]
                            .Should()
                            .BeEquivalentTo(item);
                    }

                    colNumber++;
                }
                rowNumber++;
            }
        }

        protected void AndTheTableNameIs(string tableName)
            => DataTableBuilder
                .TableName
                .Should()
                .Be(tableName);

        protected static int NewRandomYear() => NewRandomDateTime().Year;

        protected static string NewRandomString() => new RandomString();

        protected static string NewRandomStringWithMaxLength(int maxLength)
        {
            string randomString = NewRandomString();

            return randomString[..Math.Min(maxLength, randomString.Length)];
        }

        protected static string NewRandomMonth() => NewRandomDateTime().ToString("MMM");

        private static DateTime NewRandomDateTime() => new RandomDateTime();

        protected static uint NewRandomUnsignedNumber() => (uint)NewRandomNumber();

        protected static int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);

        protected static bool NewRandomFlag() => new RandomBoolean();

        protected static object[] NewRow(params object[] values) => values;

        protected object DbNullSafe(object value)
            => value ?? DBNull.Value;
    }
}