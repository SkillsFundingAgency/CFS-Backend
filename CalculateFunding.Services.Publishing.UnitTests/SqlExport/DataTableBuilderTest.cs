using System;
using System.Data;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    public abstract class DataTableBuilderTest<TBuilder>
        where TBuilder : IDataTableBuilder<PublishedProviderVersion>
    {
        protected TBuilder DataTableBuilder;
        protected DataTable ExpectedDataTable;

        protected string FundingStreamId;
        protected string FundingPeriodId;

        [TestInitialize]
        public void DataTableBuilderTestSetUp()
        {
            ExpectedDataTable = new DataTable();

            FundingStreamId = NewRandomStringWithMaxLength(32);
            FundingPeriodId = NewRandomStringWithMaxLength(32);
        }

        protected T[] AsArray<T>(params T[] items) => items;

        protected void WhenTheRowsAreAdded(params PublishedProviderVersion[] rows)
            => DataTableBuilder.AddRows(rows);

        protected FundingCalculation NewFundingCalculation(Action<FundingCalculationBuilder> setUp = null)
        {
            FundingCalculationBuilder fundingCalculationBuilder = new FundingCalculationBuilder();

            setUp?.Invoke(fundingCalculationBuilder);

            return fundingCalculationBuilder.Build();
        }

        protected Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        protected FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        protected DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        protected ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
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

        protected void ThenTheFundingLineDataTableHasColumnsMatching(params DataColumn[] expectedColumns)
        {
            ExpectedDataTable.Columns.AddRange(expectedColumns);

            DataTableBuilder.DataTable.Columns.Count
                .Should()
                .Be(ExpectedDataTable.Columns.Count);

            foreach(DataColumn col in ExpectedDataTable.Columns)
            {
                DataTableBuilder.DataTable.Columns.Contains(col.ColumnName)
                    .Should()
                    .BeTrue();
            }
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
                    if (item is not DBNull)
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

            return randomString.Substring(0, Math.Min(maxLength, randomString.Length));
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