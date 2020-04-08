using System.Collections.Generic;
using CalculateFunding.Common.CosmosDb;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingQueryBuilderTests
    {
        private PublishedFundingQueryBuilder _queryBuilder;

        [TestInitialize]
        public void SetUp()
        {
            _queryBuilder = new PublishedFundingQueryBuilder();
        }

        [TestMethod]
        [DynamicData(nameof(CountQueryExamples), DynamicDataSourceType.Method)]
        public void BuildsUpCosmosSqlToGetCountForSuppliedParameters(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            string expectedSql)
        {
            CosmosDbQuery query = _queryBuilder.BuildCountQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons);

            query
                .Should()
                .NotBeNull();

            query
                .QueryText
                .Trim()
                .Should()
                .Be(expectedSql);
        }

        [TestMethod]
        [DynamicData(nameof(QueryExamples), DynamicDataSourceType.Method)]
        public void BuildsUpCosmosSqlForSuppliedParameters(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            int top,
            int? pageRef,
            string expectedSql)
        {
            CosmosDbQuery query = _queryBuilder.BuildQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                top,
                pageRef);

            query
                .Should()
                .NotBeNull();

            query
                .QueryText
                .Trim()
                .Should()
                .Be(expectedSql);
        }

        private static IEnumerable<object[]> QueryExamples()
        {
            yield return new object[]
            {
                EnumerableFor("DSG", "PSG"),
                null,
                null,
                100,
                2,
                @"SELECT
                    p.content.id,
                    p.content.statusChangedDate,
                    p.content.fundingStreamId,
                    p.content.fundingPeriod.id AS FundingPeriodId,
                    p.content.groupingReason AS GroupingType,
                    p.content.organisationGroupTypeCode AS GroupTypeIdentifier,
                    p.content.organisationGroupIdentifierValue AS IdentifierValue,
                    p.content.version,
                    CONCAT(p.content.fundingStreamId, '-', 
                            p.content.fundingPeriod.id, '-',
                            p.content.groupingReason, '-',
                            p.content.organisationGroupTypeCode, '-',
                            ToString(p.content.organisationGroupIdentifierValue), '-',
                            ToString(p.content.majorVersion), '_',
                            ToString(p.content.minorVersion), '.json')
                    AS DocumentPath,
                    p.deleted
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('DSG','PSG')
                
                
                ORDER BY p.documentType,
				p.content.statusChangedDate, 
				p.content.id,
				p.content.fundingStreamId,
				p.content.fundingPeriod.id,
				p.content.groupingReason,
				p.deleted
                OFFSET 100 LIMIT 100"
            };
            yield return new object[]
            {
                EnumerableFor("DSG"),
                EnumerableFor("AY-2020"),
                null,
                100,
                2,
                @"SELECT
                    p.content.id,
                    p.content.statusChangedDate,
                    p.content.fundingStreamId,
                    p.content.fundingPeriod.id AS FundingPeriodId,
                    p.content.groupingReason AS GroupingType,
                    p.content.organisationGroupTypeCode AS GroupTypeIdentifier,
                    p.content.organisationGroupIdentifierValue AS IdentifierValue,
                    p.content.version,
                    CONCAT(p.content.fundingStreamId, '-', 
                            p.content.fundingPeriod.id, '-',
                            p.content.groupingReason, '-',
                            p.content.organisationGroupTypeCode, '-',
                            ToString(p.content.organisationGroupIdentifierValue), '-',
                            ToString(p.content.majorVersion), '_',
                            ToString(p.content.minorVersion), '.json')
                    AS DocumentPath,
                    p.deleted
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('DSG')
                AND p.content.fundingPeriod.id IN ('AY-2020')
                
                ORDER BY p.documentType,
				p.content.statusChangedDate, 
				p.content.id,
				p.content.fundingStreamId,
				p.content.fundingPeriod.id,
				p.content.groupingReason,
				p.deleted
                OFFSET 100 LIMIT 100"
            };
            yield return new object[]
            {
                EnumerableFor("PSG"),
                null,
                EnumerableFor("Payment", "Information"),
                60,
                5,
                @"SELECT
                    p.content.id,
                    p.content.statusChangedDate,
                    p.content.fundingStreamId,
                    p.content.fundingPeriod.id AS FundingPeriodId,
                    p.content.groupingReason AS GroupingType,
                    p.content.organisationGroupTypeCode AS GroupTypeIdentifier,
                    p.content.organisationGroupIdentifierValue AS IdentifierValue,
                    p.content.version,
                    CONCAT(p.content.fundingStreamId, '-', 
                            p.content.fundingPeriod.id, '-',
                            p.content.groupingReason, '-',
                            p.content.organisationGroupTypeCode, '-',
                            ToString(p.content.organisationGroupIdentifierValue), '-',
                            ToString(p.content.majorVersion), '_',
                            ToString(p.content.minorVersion), '.json')
                    AS DocumentPath,
                    p.deleted
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('PSG')
                
                AND p.content.groupingReason IN ('Payment','Information')
                ORDER BY p.documentType,
				p.content.statusChangedDate, 
				p.content.id,
				p.content.fundingStreamId,
				p.content.fundingPeriod.id,
				p.content.groupingReason,
				p.deleted
                OFFSET 240 LIMIT 60"
            };
            yield return new object[]
            {
                EnumerableFor("DSG", "PSG"),
                EnumerableFor("AY-1921", "AY-2020"),
                EnumerableFor("Payment", "Information"),
                100,
                4,
                @"SELECT
                    p.content.id,
                    p.content.statusChangedDate,
                    p.content.fundingStreamId,
                    p.content.fundingPeriod.id AS FundingPeriodId,
                    p.content.groupingReason AS GroupingType,
                    p.content.organisationGroupTypeCode AS GroupTypeIdentifier,
                    p.content.organisationGroupIdentifierValue AS IdentifierValue,
                    p.content.version,
                    CONCAT(p.content.fundingStreamId, '-', 
                            p.content.fundingPeriod.id, '-',
                            p.content.groupingReason, '-',
                            p.content.organisationGroupTypeCode, '-',
                            ToString(p.content.organisationGroupIdentifierValue), '-',
                            ToString(p.content.majorVersion), '_',
                            ToString(p.content.minorVersion), '.json')
                    AS DocumentPath,
                    p.deleted
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('DSG','PSG')
                AND p.content.fundingPeriod.id IN ('AY-1921','AY-2020')
                AND p.content.groupingReason IN ('Payment','Information')
                ORDER BY p.documentType,
				p.content.statusChangedDate, 
				p.content.id,
				p.content.fundingStreamId,
				p.content.fundingPeriod.id,
				p.content.groupingReason,
				p.deleted
                OFFSET 300 LIMIT 100"
            };
        }

        private static IEnumerable<object[]> CountQueryExamples()
        {
            yield return new object[]
            {
                EnumerableFor("DSG", "PSG"),
                null,
                null,
                @"SELECT
                   VALUE COUNT(1)
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('DSG','PSG')"
            };
            yield return new object[]
            {
                EnumerableFor("DSG"),
                EnumerableFor("AY-2020"),
                null,
                @"SELECT
                   VALUE COUNT(1)
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('DSG')
                AND p.content.fundingPeriod.id IN ('AY-2020')"
            };
            yield return new object[]
            {
                EnumerableFor("PSG"),
                null,
                EnumerableFor("Payment", "Information"),
                @"SELECT
                   VALUE COUNT(1)
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('PSG')
                
                AND p.content.groupingReason IN ('Payment','Information')"
            };
            yield return new object[]
            {
                EnumerableFor("DSG", "PSG"),
                EnumerableFor("AY-1921", "AY-2020"),
                EnumerableFor("Payment", "Information"),
                @"SELECT
                   VALUE COUNT(1)
                FROM publishedFunding p
                WHERE p.documentType = 'PublishedFundingVersion'
                AND p.deleted = false
                AND p.content.fundingStreamId IN ('DSG','PSG')
                AND p.content.fundingPeriod.id IN ('AY-1921','AY-2020')
                AND p.content.groupingReason IN ('Payment','Information')"
            };
        }

        private static string[] EnumerableFor(params string[] ids) => ids;
    }
}