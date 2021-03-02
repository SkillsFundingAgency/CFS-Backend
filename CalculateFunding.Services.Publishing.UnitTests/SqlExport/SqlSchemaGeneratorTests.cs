using System;
using System.Collections.Generic;
using CalculateFunding.Models.Publishing.SqlExport;
using CalculateFunding.Services.Publishing.SqlExport;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlSchemaGeneratorTests
    {
        private SqlSchemaGenerator _sqlSchemaGenerator;


        [TestInitialize]
        public void SetUp()
        {
            _sqlSchemaGenerator = new SqlSchemaGenerator();
        }

        [TestMethod]
        [DynamicData(nameof(GenerateDDLExamples), DynamicDataSourceType.Method)]
        public void GeneratesDDLForSuppliedTableDefinitions(string tableName,
            string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<SqlColumnDefinition> columnDefinitions,
            string expectedDDL)
        {
            TheGeneratedDDLFor(tableName, fundingStreamId, fundingPeriodId, columnDefinitions)
                .Should()
                .Be(expectedDDL);
        }

        private string TheGeneratedDDLFor(string tableName,
            string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<SqlColumnDefinition> columnDefinitions)
            => _sqlSchemaGenerator.GenerateCreateTableSql(tableName,
                fundingStreamId,
                fundingPeriodId,
                columnDefinitions);

        private static IEnumerable<object[]> GenerateDDLExamples()
        {
            yield return new object[]
            {
                "TableOne",
                "streamOne",
                "periodOne",
                AsArray(NewColumn(_ => _.WithName("one")
                        .WithType("boolean")
                        .WithAllowNulls(false)),
                    NewColumn(_ => _.WithName("two")
                        .WithType("string")
                        .WithAllowNulls(false)),
                    NewColumn(_ => _.WithName("three")
                        .WithType("decimal")
                        .WithAllowNulls(true))),
                @" CREATE TABLE[dbo].[TableOne](
           
               [PublishedProviderId][varchar](128) NOT NULL,
          
              [one]boolean NOT NULL,
[two]string NOT NULL,
[three]decimal NULL,

 CONSTRAINT[PK_TableOne_1] PRIMARY KEY CLUSTERED
(

   [PublishedProviderId] ASC
)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]
) ON[PRIMARY];

EXEC sys.sp_addextendedproperty   
@name = N'CFS_FundingStreamId_FundingPeriodId',   
@value = N'streamOne_periodOne',   
@level0type = N'SCHEMA', @level0name = 'dbo',  
@level1type = N'TABLE',  @level1name = 'TableOne';"
            };
            yield return new object[]
            {
                "TableOne",
                "streamTwo",
                "periodTwo",
                AsArray(NewColumn(_ => _.WithName("one")
                        .WithType("decimal")
                        .WithAllowNulls(true)),
                    NewColumn(_ => _.WithName("two")
                        .WithType("string")
                        .WithAllowNulls(false))),
                @" CREATE TABLE[dbo].[TableOne](
           
               [PublishedProviderId][varchar](128) NOT NULL,
          
              [one]decimal NULL,
[two]string NOT NULL,

 CONSTRAINT[PK_TableOne_1] PRIMARY KEY CLUSTERED
(

   [PublishedProviderId] ASC
)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]
) ON[PRIMARY];

EXEC sys.sp_addextendedproperty   
@name = N'CFS_FundingStreamId_FundingPeriodId',   
@value = N'streamTwo_periodTwo',   
@level0type = N'SCHEMA', @level0name = 'dbo',  
@level1type = N'TABLE',  @level1name = 'TableOne';"
            };
        }

        private static SqlColumnDefinition NewColumn(Action<SqlColumnDefinitionBuilder> setUp = null)
        {
            SqlColumnDefinitionBuilder sqlColumnDefinitionBuilder = new SqlColumnDefinitionBuilder();

            setUp?.Invoke(sqlColumnDefinitionBuilder);

            return sqlColumnDefinitionBuilder.Build();
        }

        private static SqlColumnDefinition[] AsArray(params SqlColumnDefinition[] columnDefinitions)
            => columnDefinitions;
    }
}