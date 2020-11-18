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
            IEnumerable<SqlColumnDefinition> columnDefinitions,
            string expectedDDL)
        {
            TheGeneratedDDLFor(tableName, columnDefinitions)
                .Should()
                .Be(expectedDDL);
        }

        private string TheGeneratedDDLFor(string tableName,
            IEnumerable<SqlColumnDefinition> columnDefinitions)
            => _sqlSchemaGenerator.GenerateCreateTableSql(tableName, columnDefinitions);

        private static IEnumerable<object[]> GenerateDDLExamples()
        {
            yield return new object [] { "TableOne", AsArray(NewColumn(_ => _.WithName("one")
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
) ON[PRIMARY]"
            };
            yield return new object [] { "TableOne", AsArray(NewColumn(_ => _.WithName("one")
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
) ON[PRIMARY]"
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