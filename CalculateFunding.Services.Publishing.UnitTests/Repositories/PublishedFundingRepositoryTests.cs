using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Repositories
{
    [TestClass]
    public class PublishedFundingRepositoryTests
    {
        private ICosmosRepository _cosmosRepository;
        private Mock<IPublishedFundingQueryBuilder> _publishedFundingQueryBuilder;

        private PublishedFundingRepository _repository;

        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _publishedProviderVersion;

        [TestInitialize]
        public void SetUp()
        {
            _cosmosRepository = Substitute.For<ICosmosRepository>();
            _publishedFundingQueryBuilder = new Mock<IPublishedFundingQueryBuilder>();

            _repository = new PublishedFundingRepository(_cosmosRepository,
                _publishedFundingQueryBuilder.Object);

            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _publishedProviderVersion = NewRandomString();
        }

        [TestMethod]
        public async Task IsHealthOkChecksCosmosRepository()
        {
            bool expectedIsOkFlag = new RandomBoolean();
            string expectedMessage = new RandomString();

            GivenTheRepositoryServiceHealth(expectedIsOkFlag, expectedMessage);

            ServiceHealth isHealthOk = await _repository.IsHealthOk();

            isHealthOk
                .Should()
                .NotBeNull();

            isHealthOk
                .Name
                .Should()
                .Be(nameof(PublishedFundingRepository));

            DependencyHealth dependencyHealth = isHealthOk
                .Dependencies
                .FirstOrDefault();

            dependencyHealth
                .Should()
                .Match<DependencyHealth>(_ => _.HealthOk == expectedIsOkFlag &&
                                              _.Message == expectedMessage);
        }

        [TestMethod]
        public async Task GetLatestPublishedDateGetsMaxUpdateAtForPublishedProvidersByFundingStreamAndPeriod()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            DateTime expectedLastPublishedDate = new RandomDateTime();

            GivenTheDynamicResultsForTheQuery(_ => _.QueryText?.Equals(@"SELECT MAX(c.updatedAt)
                                  FROM c
                                  WHERE c.documentType = 'PublishedProvider'
                                  AND c.deleted = false
                                  AND c.content.current.fundingStreamId = @fundingStreamId
                                  AND c.content.current.fundingPeriodId = @fundingPeriodId") == true &&
                HasParameter(_, "@fundingPeriodId", fundingPeriodId) &&
                HasParameter(_, "@fundingStreamId", fundingStreamId),
                JObject.Parse($"{{\"$1\" : \"{expectedLastPublishedDate.ToString(CultureInfo.InvariantCulture)}\"}}"));

            DateTime? actualLastPublishedDate = await _repository.GetLatestPublishedDate(fundingStreamId, fundingPeriodId);

            actualLastPublishedDate
                .Should()
                .BeCloseTo(expectedLastPublishedDate, 999);
        }

        [TestMethod]
        public async Task QueryPublishedFundingDelegatesToQueryBuilderAndExecutesCosmosDbQueryItCreates()
        {
            IEnumerable<string> fundingStreamIds = EnumerableFor(NewRandomString(), NewRandomString());
            IEnumerable<string> fundingPeriodIds = EnumerableFor(NewRandomString(), NewRandomString(), NewRandomString());
            IEnumerable<string> groupingReasons = EnumerableFor(NewRandomString());
            IEnumerable<string> variationReasons = EnumerableFor(NewRandomString());
            int top = NewRandomNumber();
            int pageRef = NewRandomNumber();


            IEnumerable<PublishedFundingIndex> expectedResults = new PublishedFundingIndex[0];
            CosmosDbQuery query = new CosmosDbQuery();

            GivenTheCosmosDbQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons,
                top,
                pageRef,
                query);
            AndTheDynamicResultsForTheQuery(query, expectedResults);

            IEnumerable<PublishedFundingIndex> actualResults = await _repository.QueryPublishedFunding(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons,
                top,
                pageRef,
                0);

            actualResults
                .Should()
                .BeEquivalentTo(expectedResults);
        }

        [TestMethod]
        public async Task QueryPublishedFundingCountBuildsQueryAndTreatsResultsAsScalarCountJObject()
        {
            IEnumerable<string> fundingStreamIds = EnumerableFor(NewRandomString(), NewRandomString());
            IEnumerable<string> fundingPeriodIds = EnumerableFor(NewRandomString(), NewRandomString(), NewRandomString());
            IEnumerable<string> groupingReasons = EnumerableFor(NewRandomString());
            IEnumerable<string> variationReasons = EnumerableFor(NewRandomString());

            CosmosDbQuery query = new CosmosDbQuery();

            GivenTheCosmosDbCountQuery(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons,
                query);

            int expectedCount = new RandomNumberBetween(1, 10000);
            IEnumerable<dynamic> results = new dynamic[] { expectedCount };

            AndTheDynamicResultsForTheQuery(query, results);

            int actualCount = await _repository.QueryPublishedFundingCount(fundingStreamIds,
                fundingPeriodIds,
                groupingReasons,
                variationReasons);

            actualCount
                .Should()
                .Be(expectedCount);
        }

        private void GivenTheCosmosDbQuery(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef,
            CosmosDbQuery expectedQuery)
        {
            _publishedFundingQueryBuilder.Setup(_ => _.BuildQuery(fundingStreamIds,
                    fundingPeriodIds,
                    groupingReasons,
                    variationReasons,
                    top,
                    pageRef,
                    0))
                .Returns(expectedQuery);
        }

        private void GivenTheCosmosDbCountQuery(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            CosmosDbQuery expectedQuery)
        {
            _publishedFundingQueryBuilder.Setup(_ => _.BuildCountQuery(fundingStreamIds,
                    fundingPeriodIds,
                    groupingReasons,
                    variationReasons))
                .Returns(expectedQuery);
        }

        private void AndTheDynamicResultsForTheQuery(CosmosDbQuery query, IEnumerable<dynamic> expectedResults)
        {
            _cosmosRepository
                .DynamicQuery(query)
                .Returns(expectedResults);
        }

        private int NewRandomNumber() => new RandomNumberBetween(1, 10000);

        private bool NewRandomFlag() => new RandomBoolean();


        private IEnumerable<string> EnumerableFor(params string[] items) => items;

        [TestMethod]
        public async Task DeleteAllPublishedProvidersByFundingStreamAndPeriodBulkDeletesAllDocumentsWithMatchingFundingPeriodAndStream()
        {
            await WhenThePublishedProvidersAreDeleted();

            const string queryText = @"SELECT
                                        c.content.id,
                                        { 
                                           'id' : c.content.current.id,
                                           'providerId' : c.content.current.providerId,
                                           'fundingStreamId' : c.content.current.fundingStreamId,
                                           'fundingPeriodId' : c.content.current.fundingPeriodId
                                        } AS Current
                               FROM     publishedProvider c
                               WHERE    c.documentType = 'PublishedProvider'
                               AND      c.content.current.fundingStreamId = @fundingStreamId
                               AND      c.content.current.fundingPeriodId = @fundingPeriodId";

            await _cosmosRepository
                .Received(1)
                .DocumentsBatchProcessingAsync(Arg.Any<Func<List<PublishedProvider>, Task>>(),
                    Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                                               HasParameter(_, "@fundingStreamId", _fundingStreamId) &&
                                               HasParameter(_, "@fundingPeriodId", _fundingPeriodId)),
                50);
        }

        [TestMethod]
        public async Task DeleteAllPublishedProviderVersionsByFundingStreamAndPeriodBulkDeletesAllDocumentsWithMatchingFundingPeriodAndStream()
        {
            await WhenThePublishedProviderVersionsAreDeleted();

            const string queryText = @"SELECT
                                        c.content.id,
                                        { 
                                           'providerType' : c.content.provider.providerType,
                                           'authority' : c.content.provider.authority,
                                           'name' : c.content.provider.name
                                        } AS Provider,
                                        c.content.status,
                                        c.content.totalFunding,
                                        c.content.specificationId,
                                        c.content.fundingStreamId,
                                        c.content.providerId,
                                        c.content.fundingPeriodId,
                                        c.content.version,
                                        c.content.majorVersion,
                                        c.content.minorVersion
                               FROM     publishedProviderVersion c
                               WHERE    c.documentType = 'PublishedProviderVersion'
                               AND      c.content.fundingStreamId = @fundingStreamId
                               AND      c.content.fundingPeriodId = @fundingPeriodId";

            await _cosmosRepository
                .Received(1)
                .DocumentsBatchProcessingAsync(Arg.Any<Func<List<PublishedProviderVersion>, Task>>(),
                    Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                                               HasParameter(_, "@fundingStreamId", _fundingStreamId) &&
                                               HasParameter(_, "@fundingPeriodId", _fundingPeriodId)),
                    50);
        }

        [TestMethod]
        public async Task DeleteAllPublishedFundingsByFundingStreamAndPeriodBulkDeletesAllDocumentsWithMatchingFundingPeriodAndStream()
        {
            await WhenThePublishedFundingsAreDeleted();

            const string queryText = @"SELECT
                                    {
                                        'id':c.content.id,
                                        'current' : {
                                            'groupingReason':c.content.current.groupingReason,
                                            'organisationGroupTypeCode':c.content.current.organisationGroupTypeCode,
                                            'organisationGroupIdentifierValue':c.content.current.organisationGroupIdentifierValue,
                                            'fundingStreamId':c.content.current.fundingStreamId,
                                            'fundingPeriod':{
                                                        'id':c.content.current.fundingPeriod.id,
                                                        'type':c.content.current.fundingPeriod.type,
                                                        'period':c.content.current.fundingPeriod.period
                                            }
                                        }
                                    } AS content
                               FROM     publishedFunding c
                               WHERE    c.documentType = 'PublishedFunding'
                               AND      c.content.current.fundingStreamId = @fundingStreamId
                               AND      c.content.current.fundingPeriod.id = @fundingPeriodId";

            await _cosmosRepository
                .Received(1)
                .DocumentsBatchProcessingAsync(Arg.Any<Func<List<DocumentEntity<PublishedFunding>>, Task>>(),
                    Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                                               HasParameter(_, "@fundingStreamId", _fundingStreamId) &&
                                               HasParameter(_, "@fundingPeriodId", _fundingPeriodId)),
                    50);
        }

        [TestMethod]
        public async Task DeleteAllPublishedFundingVersionsByFundingStreamAndPeriodBulkDeletesAllDocumentsWithMatchingFundingPeriodAndStream()
        {
            await WhenThePublishedFundingVersionsAreDeleted();

            const string queryText = @"SELECT
                                    { 
                                        'id':c.content.id,
                                        'groupingReason':c.content.groupingReason,
                                        'organisationGroupTypeCode':c.content.organisationGroupTypeCode,
                                        'organisationGroupIdentifierValue':c.content.organisationGroupIdentifierValue,
                                        'version':c.content.version,
                                        'fundingStreamId':c.content.fundingStreamId,
                                        'fundingPeriod':{
                                            'id':c.content.fundingPeriod.id,
                                            'type':c.content.fundingPeriod.type,
                                            'period':c.content.fundingPeriod.period
                                        }
                                    } as content
                               FROM     publishedFundingVersion c
                               WHERE    c.documentType = 'PublishedFundingVersion'
                               AND      c.content.fundingStreamId = @fundingStreamId
                               AND      c.content.fundingPeriod.id = @fundingPeriodId";

            await _cosmosRepository
                .Received(1)
                .DocumentsBatchProcessingAsync(Arg.Any<Func<List<DocumentEntity<PublishedFundingVersion>>, Task>>(),
                    Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                                               HasParameter(_, "@fundingStreamId", _fundingStreamId) &&
                                               HasParameter(_, "@fundingPeriodId", _fundingPeriodId)),
                    50);
        }

        [TestMethod]
        public async Task GetPublishedProviderIdRetrievesPublishedProvider()
        {
            await WhenThePublishedProviderVersionsAreRetrieved();

            const string queryText = @"
                                SELECT c.content.released.provider.providerVersionId, 
                                c.content.released.provider.providerId 
                                FROM c
                                WHERE c.documentType = 'PublishedProvider'
                                AND c.content.released.fundingId = @publishedProviderVersion";

            await _cosmosRepository
                .Received(1)
                .DynamicQuery(Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                    HasParameter(_, "@publishedProviderVersion", _publishedProviderVersion)));
        }

        [TestMethod]
        public void PublishedFundingVersionBatchProcessing()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            int batchSize = NewRandomNumber();

            WhenPublishedFundingVersionBatchProcessingIsUsed(specificationId,
                fundingStreamId,
                fundingPeriodId,
                batchSize);

            string queryText = @"SELECT 
                                {
                                    'id':c.content.id,
                                    'organisationGroupTypeCode':c.content.organisationGroupTypeCode,
                                    'organisationGroupName':c.content.organisationGroupName,
                                    'fundingStreamId':c.content.fundingStreamId,
                                    'fundingPeriod':{
                                          'id' : c.content.fundingPeriod.id
                                    },
                                    'specificationId':c.content.specificationId,
                                    'status':c.content.status,
                                    'version':c.content.version,
                                    'majorVersion':c.content.majorVersion,
                                    'minorVersion':c.content.minorVersion,
                                    'date':c.content.date,
                                    'author':{
                                        'name' : c.content.author.name
                                    },
                                    'fundingLines':ARRAY(
                                        SELECT fundingLine.name,
                                        fundingLine['value']
                                        FROM fundingLine IN c.content.fundingLines
                                    ),
                                    'providerFundings':c.content.providerFundings
                                } as content
                                FROM     publishedFundingVersions c
                                WHERE    c.documentType = 'PublishedFundingVersion'
                                AND      c.content.specificationId = @specificationId
                                AND      c.content.fundingPeriod.id = @fundingPeriodId
                                AND      c.content.fundingStreamId = @fundingStreamId
                                AND      c.deleted = false
                                ORDER BY c.content.organisationGroupTypeCode ASC,
                                c.content.date DESC";

            _cosmosRepository
                .Received(1)
                .GetFeedIterator(
                    Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                                               HasParameter(_, "@fundingPeriodId", fundingPeriodId) &&
                                               HasParameter(_, "@fundingStreamId", fundingStreamId) &&
                                               HasParameter(_, "@specificationId", specificationId)),
                    Arg.Is(batchSize));
        }

        private void WhenPublishedFundingVersionBatchProcessingIsUsed(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            int batchSize)
            => _repository.GetPublishedFundingVersionsForBatchProcessing(specificationId,
                fundingStreamId,
                fundingPeriodId,
                batchSize);

        [TestMethod]
        public void PublishedFundingBatchProcessing()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            int batchSize = NewRandomNumber();

            WhenPublishedFundingBatchProcessingIsUsed(specificationId,
                fundingStreamId,
                fundingPeriodId,
                batchSize);

            string queryText = @"SELECT 
                                {
                                    'id':c.content.id,
                                    'current':{
                                        'organisationGroupTypeCode' : c.content.current.organisationGroupTypeCode,
                                        'groupingReason' : c.content.current.groupingReason,
                                        'organisationGroupName' : c.content.current.organisationGroupName,
                                        'organisationGroupIdentifierValue' : c.content.current.organisationGroupIdentifierValue,
                                        'fundingStreamId' : c.content.current.fundingStreamId,
                                        'fundingPeriod' : {
                                          'id' : c.content.current.fundingPeriod.id
                                        },
                                        'specificationId' : c.content.current.specificationId,
                                        'status' : c.content.current.status,
                                        'version' : c.content.current.version,
                                        'majorVersion' : c.content.current.majorVersion,
                                        'minorVersion' : c.content.current.minorVersion,
                                        'date' : c.content.current.date,
                                        'providerFundings' : c.content.current.providerFundings,
                                        'author' : {
                                            'name' : c.content.current.author.name
                                        },
                                        'fundingLines' : ARRAY(
                                            SELECT fundingLine.name,
                                            fundingLine['value']
                                            FROM fundingLine IN c.content.current.fundingLines
                                        )
                                    }
                                } as content
                                FROM     publishedFunding c
                                WHERE    c.documentType = 'PublishedFunding'
                                AND      c.content.current.specificationId = @specificationId
                                AND      c.content.current.fundingPeriod.id = @fundingPeriodId
                                AND      c.content.current.fundingStreamId = @fundingStreamId
                                AND      c.deleted = false
                                ORDER BY c.content.current.organisationGroupTypeCode ASC,
                                c.content.current.date DESC";

            _cosmosRepository
                .Received(1)
                .GetFeedIterator(
                    Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText &&
                                               HasParameter(_, "@fundingPeriodId", fundingPeriodId) &&
                                               HasParameter(_, "@fundingStreamId", fundingStreamId) &&
                                               HasParameter(_, "@specificationId", specificationId)),
                    Arg.Is(batchSize));
        }

        private void WhenPublishedFundingBatchProcessingIsUsed(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            int batchSize)
            => _repository.GetPublishedFundingForBatchProcessing(specificationId,
                fundingStreamId,
                fundingPeriodId,
                batchSize);

        [TestMethod]
        public async Task PublishedGroupBatchProcessingShouldRetrieveBySpecificationId()
        {
            const string specificationId = "spec-1";

            await WhenPublishedGroupBatchProcessing(specificationId, (List<PublishedFunding> pfs) =>
            {
                int count = pfs.Count();
                return Task.CompletedTask;
            }, 50);

            string queryText = $@"SELECT {{
                                'fundingId': c.content.current.fundingId,
                                'majorVersion': c.content.current.majorVersion,
                                'groupingReason': c.content.current.groupingReason,
                                'organisationGroupTypeCode' : c.content.current.organisationGroupTypeCode,
                                'organisationGroupName' : c.content.current.organisationGroupName,
                                'organisationGroupTypeIdentifier' : c.content.current.organisationGroupTypeIdentifier,
                                'organisationGroupIdentifierValue' : c.content.current.organisationGroupIdentifierValue,
                                'organisationGroupTypeClassification' : c.content.current.organisationGroupTypeClassification,
                                'totalFunding' : c.content.current.totalFunding,
                                'author' : {{
                                                'name' : c.content.current.author.name
                                            }},
                                'statusChangedDate' : c.content.current.statusChangedDate,
                                'providerFundings' : c.content.current.providerFundings
                                }} AS current                                
                                FROM c 
                                where c.documentType='PublishedFunding'
                                and c.content.current.status = 'Released'
                                and c.content.current.specificationId = @specificationId";

            await _cosmosRepository
                .Received(1)
                .DocumentsBatchProcessingAsync(
                Arg.Any<Func<List<PublishedFunding>, Task>>(),
                Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText && HasParameter(_, "@specificationId", specificationId)),
                50);

        }

        [TestMethod]
        public async Task QueryPublishedProviderShouldRetrievePublishedProvidersBySpecificationAndFundingIds()
        {
            const string specificationId = "spec-1";
            IEnumerable<string> fundingIds = new[] { "fid1", "fid2" };

            await WhenQueryPublishedProvider(specificationId, fundingIds);

            string queryText = $@"SELECT 
                            c.content.released.fundingId,
                            c.content.released.fundingStreamId,
                            c.content.released.fundingPeriodId,
                            c.content.released.provider.providerId,
                            c.content.released.provider.name AS providerName,
                            c.content.released.majorVersion,
                            c.content.released.minorVersion,
                            c.content.released.totalFunding,
                            c.content.released.provider.ukprn,
                            c.content.released.provider.urn,
                            c.content.released.provider.upin,
                            c.content.released.provider.laCode,
                            c.content.released.provider.status,
                            c.content.released.provider.successor,
                            c.content.released.predecessors,
                            c.content.released.variationReasons
                        FROM c where c.documentType='PublishedProvider' 
                        and c.content.released.specificationId = @specificationId
                        and ARRAY_CONTAINS (@fundingIds, c.content.released.fundingId)";

            await _cosmosRepository
                .Received(1)
                .DynamicQuery(
                Arg.Is<CosmosDbQuery>(_ => _.QueryText == queryText
                && HasParameter(_, "@specificationId", specificationId)
                && HasArrayParameter(_, "@fundingIds", fundingIds)));
        }

        [TestMethod]
        public void GetPublishedProviderStatusCountGuardsAgainstMissingSpecificationId()
        {
            Func<Task<IEnumerable<PublishedProviderFunding>>> invocation = () => WhenThePublishedProviderFundingIsQueried(AsArray(NewRandomString()),
                null,
                NewRandomStatus());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public void GetPublishedProviderStatusCountGuardsAgainstEmptyPublishedProviderIds()
        {
            Func<Task<IEnumerable<PublishedProviderFunding>>> invocation = () => WhenThePublishedProviderFundingIsQueried(AsArray<string>(),
                NewRandomString(),
                NewRandomStatus());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("publishedProviderIds");
        }

        [TestMethod]
        public void GetPublishedProviderStatusCountGuardsAgainstTooManyPublishedProviderIds()
        {
            Func<Task<IEnumerable<PublishedProviderFunding>>> invocation = () => WhenThePublishedProviderFundingIsQueried(new string[101],
                NewRandomString(),
                NewRandomStatus());

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("You can only filter against 100 published provider ids at a time");
        }

        [TestMethod]
        public void GetPublishedProviderStatusCountGuardsAgainstMissingPublishedProviderIds()
        {
            Func<Task<IEnumerable<PublishedProviderFunding>>> invocation = () => WhenThePublishedProviderFundingIsQueried(null,
                NewRandomString(),
                NewRandomStatus());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("publishedProviderIds");
        }

        [TestMethod]
        public void GetPublishedProviderStatusCountGuardsAgainstMissingStatuses()
        {
            Func<Task<IEnumerable<PublishedProviderFunding>>> invocation = () => WhenThePublishedProviderFundingIsQueried(AsArray(NewRandomString()),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("statuses");
        }

        [TestMethod]
        public async Task GetPublishedProvidesFunding()
        {
            string publishedProviderId0 = NewRandomString();
            string publishedProviderId1 = NewRandomString();
            string publishedProviderId2 = NewRandomString();

            string[] publishedProviderIds = AsArray(publishedProviderId0, publishedProviderId1, publishedProviderId2);

            PublishedProviderStatus status0 = NewRandomStatus();
            PublishedProviderStatus status1 = NewRandomStatus();

            PublishedProviderStatus[] statuses = AsArray(status0, status1);
            string specificationId = NewRandomString();

            List<dynamic> results = new List<dynamic>() {
                CreatePublishedProviderFundingResult(specificationId, publishedProviderId0, false),
                CreatePublishedProviderFundingResult(specificationId, publishedProviderId1, false),
                CreatePublishedProviderFundingResult(specificationId, publishedProviderId2)
            };

            GivenTheDynamicResultsForTheQuery(QueryMatch(specificationId, publishedProviderIds, statuses), results);

            IEnumerable<PublishedProviderFunding> fundings = await WhenThePublishedProviderFundingIsQueried(publishedProviderIds, specificationId, statuses);

            fundings
                .Count()
                .Should()
                .Be(3);

            Func<CosmosDbQuery, bool> QueryMatch(string s,
                string[] strings,
                PublishedProviderStatus[] publishedProviderStatuses) =>
                _ => _.QueryText == @"
                              SELECT 
                                  c.content.current.specificationId,
                                  c.content.current.publishedProviderId,
                                  c.content.current.fundingStreamId,
                                  c.content.current.totalFunding,
                                  c.content.current.provider.providerType,
                                  c.content.current.provider.providerSubType, 
                                  c.content.current.provider.laCode,
                                  c.content.current.isIndicative
                              FROM publishedProvider c
                              WHERE c.documentType = 'PublishedProvider'
                              AND c.content.current.specificationId = @specificationId
                              AND ARRAY_CONTAINS(@publishedProviderIds, c.content.current.publishedProviderId) 
                              AND ARRAY_CONTAINS(@statuses, c.content.current.status)
                              AND (IS_NULL(c.content.current.errors) OR ARRAY_LENGTH(c.content.current.errors) = 0)
                              AND c.deleted = false" &&
                     HasParameter(_, "@specificationId", s) &&
                     HasArrayParameter(_, "@publishedProviderIds", publishedProviderIds) &&
                     HasArrayParameter(_, "@statuses", statuses.Select(status => status.ToString()));
        }

        [TestMethod]
        public void GetReleaseFundingPublishedProvidersGuardsAgainstMissingSpecificationId()
        {
            Func<Task<IEnumerable<PublishedProviderFundingSummary>>> invocation = () => WhenGetReleaseFundingPublishedProviders(AsArray(NewRandomString()),
                null,
                NewRandomStatus());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public void GetReleaseFundingPublishedProvidersGuardsAgainstTooManyPublishedProviderIds()
        {
            Func<Task<IEnumerable<PublishedProviderFundingSummary>>> invocation = () => WhenGetReleaseFundingPublishedProviders(new string[101],
                NewRandomString(),
                NewRandomStatus());

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("You can only filter against 100 published provider ids at a time");
        }

        [TestMethod]
        public void GetReleaseFundingPublishedProvidersGuardsAgainstMissingStatuses()
        {
            Func<Task<IEnumerable<PublishedProviderFundingSummary>>> invocation = () => WhenGetReleaseFundingPublishedProviders(AsArray(NewRandomString()),
                NewRandomString(),
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("statuses");
        }

        [TestMethod]
        public async Task GetReleaseFundingPublishedProviders()
        {
            string publishedProviderId0 = NewRandomString();
            string publishedProviderId1 = NewRandomString();
            string publishedProviderId2 = NewRandomString();

            string[] publishedProviderIds = AsArray(publishedProviderId0, publishedProviderId1, publishedProviderId2);

            PublishedProviderStatus status0 = NewRandomStatus();
            PublishedProviderStatus status1 = NewRandomStatus();

            PublishedProviderStatus[] statuses = AsArray(status0, status1);
            string specificationId = NewRandomString();

            List<dynamic> results = new List<dynamic>() {
                CreateReleaseFundingSummaryPublishedProviderFundingResult(specificationId, publishedProviderId0, false),
                CreateReleaseFundingSummaryPublishedProviderFundingResult(specificationId, publishedProviderId1, false),
                CreateReleaseFundingSummaryPublishedProviderFundingResult(specificationId, publishedProviderId2)
            };

            GivenTheDynamicResultsForTheQuery(QueryMatch(specificationId, statuses, publishedProviderIds), results);

            IEnumerable<PublishedProviderFundingSummary> fundings = await WhenGetReleaseFundingPublishedProviders(publishedProviderIds, specificationId, statuses);

            fundings
                .Count()
                .Should()
                .Be(3);

            Func<CosmosDbQuery, bool> QueryMatch(string s,
                PublishedProviderStatus[] publishedProviderStatuses,
                string[] strings) =>
                _ => _.QueryText == @"SELECT 
                                c.content.current.specificationId,
                                c.content.current.totalFunding,
                                c.content.current.isIndicative,
                                c.content.current.majorVersion,
                                c.content.current.minorVersion,
                                c.content.current.status,
                                c.content.current.provider
                            FROM publishedProvider c
                            WHERE c.documentType = 'PublishedProvider'
                            AND c.content.current.specificationId = @specificationId AND ARRAY_CONTAINS(@publishedProviderIds, c.content.current.publishedProviderId)
                            AND ARRAY_CONTAINS(@statuses, c.content.current.status)
                            AND (IS_NULL(c.content.current.errors) OR ARRAY_LENGTH(c.content.current.errors) = 0)
                            AND c.deleted = false" &&
                     HasParameter(_, "@specificationId", s) &&
                     HasArrayParameter(_, "@statuses", statuses.Select(status => status.ToString())) &&
                     HasArrayParameter(_, "@publishedProviderIds", publishedProviderIds);
        }

        [TestMethod]
        public async Task GetReleaseFundingPublishedProviders_NonBatchMode()
        {
            string publishedProviderId0 = NewRandomString();
            string publishedProviderId1 = NewRandomString();
            string publishedProviderId2 = NewRandomString();

            PublishedProviderStatus status0 = NewRandomStatus();
            PublishedProviderStatus status1 = NewRandomStatus();

            PublishedProviderStatus[] statuses = AsArray(status0, status1);
            string specificationId = NewRandomString();

            List<dynamic> results = new List<dynamic>() {
                CreateReleaseFundingSummaryPublishedProviderFundingResult(specificationId, publishedProviderId0, false),
                CreateReleaseFundingSummaryPublishedProviderFundingResult(specificationId, publishedProviderId1, false),
                CreateReleaseFundingSummaryPublishedProviderFundingResult(specificationId, publishedProviderId2)
            };

            GivenTheDynamicResultsForTheQuery(QueryMatch(specificationId, statuses, new string[0]), results);

            IEnumerable<PublishedProviderFundingSummary> fundings = await WhenGetReleaseFundingPublishedProviders(new string[0], specificationId, statuses);

            fundings
                .Count()
                .Should()
                .Be(3);

            Func<CosmosDbQuery, bool> QueryMatch(string s,
                PublishedProviderStatus[] publishedProviderStatuses,
                string[] strings) =>
                _ => _.QueryText == @"SELECT 
                                c.content.current.specificationId,
                                c.content.current.totalFunding,
                                c.content.current.isIndicative,
                                c.content.current.majorVersion,
                                c.content.current.minorVersion,
                                c.content.current.status,
                                c.content.current.provider
                            FROM publishedProvider c
                            WHERE c.documentType = 'PublishedProvider'
                            AND c.content.current.specificationId = @specificationId
                            AND ARRAY_CONTAINS(@statuses, c.content.current.status)
                            AND (IS_NULL(c.content.current.errors) OR ARRAY_LENGTH(c.content.current.errors) = 0)
                            AND c.deleted = false" &&
                     HasParameter(_, "@specificationId", s) &&
                     HasArrayParameter(_, "@statuses", statuses.Select(status => status.ToString()));
        }

        [TestMethod]
        public async Task GetPublishedProvidersFundingDataForCsvReport()
        {
            string publishedProviderId0 = NewRandomString();
            string publishedProviderId1 = NewRandomString();
            string publishedProviderId2 = NewRandomString();

            string[] publishedProviderIds = AsArray(publishedProviderId0, publishedProviderId1, publishedProviderId2);

            PublishedProviderStatus status0 = NewRandomStatus();
            PublishedProviderStatus status1 = NewRandomStatus();

            PublishedProviderStatus[] statuses = AsArray(status0, status1);
            string specificationId = NewRandomString();

            List<dynamic> results = new List<dynamic>() {
                CreatePublishedProviderFundingCsvData(specificationId, publishedProviderId0, status0.ToString()),
                CreatePublishedProviderFundingCsvData(specificationId, publishedProviderId1, status1.ToString(), true),
                CreatePublishedProviderFundingCsvData(specificationId, publishedProviderId2, status0.ToString())
            };

            GivenTheDynamicResultsForTheQuery(QueryMatch(specificationId, publishedProviderIds, statuses), results);

            IEnumerable<PublishedProviderFundingCsvData> fundings = await WhenThePublishedProviderFundingDataForCsvReportIsQueried(publishedProviderIds, specificationId, statuses);

            fundings
                .Count()
                .Should()
                .Be(3);

            Func<CosmosDbQuery, bool> QueryMatch(string s,
                string[] strings,
                PublishedProviderStatus[] publishedProviderStatuses) =>
                _ => _.QueryText == @"SELECT 
                                c.content.current.specificationId,
                                c.content.current.fundingStreamId, 
                                c.content.current.fundingPeriodId,
                                c.content.current.status,
                                c.content.current.provider.providerId,
                                c.content.current.provider.ukprn,
                                c.content.current.provider.urn,
                                c.content.current.provider.upin,
                                c.content.current.provider.name,
                                c.content.current.totalFunding,
                                c.content.current.majorVersion,
                                c.content.current.minorVersion,
                                c.content.current.isIndicative,
                                c.content.current.variationReasons,
                                c.content.released.majorVersion As lastReleasedMajorVersion,
                                c.content.released.minorVersion As lastReleasedMinorVersion,
                                c.content.released.totalFunding As lastReleasedTotalFunding
                            FROM publishedProvider c
                            WHERE c.documentType = 'PublishedProvider'
                            AND c.content.current.specificationId = @specificationId AND ARRAY_CONTAINS(@publishedProviderIds, c.content.current.publishedProviderId)
                            AND ARRAY_CONTAINS(@statuses, c.content.current.status)
                            AND c.deleted = false" &&
                     HasParameter(_, "@specificationId", s) &&
                     HasArrayParameter(_, "@statuses", statuses.Select(status => status.ToString())) &&
                     HasArrayParameter(_, "@publishedProviderIds", publishedProviderIds);
        }

        [TestMethod]
        public async Task RemoveIdsInErrorQueriesForMatchingPublishedProviderIdsWhereNotInError()
        {
            string idOne = NewRandomString();
            string idTwo = NewRandomString();
            string idThree = NewRandomString();
            string idFour = NewRandomString();
            string idFive = NewRandomString();

            List<dynamic> results = new List<dynamic>()
            {
                NewPublishedProviderId(idOne),
                NewPublishedProviderId(idFour),
                NewPublishedProviderId(idFive),
            };

            string[] ids = AsArray(idOne, idTwo, idThree, idFour, idFive);

            GivenTheDynamicResultsForTheQuery(QueryMatch(
                ids),
                results);

            IEnumerable<string> filteredIds = await _repository.RemoveIdsInError(ids);

            filteredIds
                .Should()
                .BeEquivalentTo(AsArray(idOne, idFour, idFive));

            Func<CosmosDbQuery, bool> QueryMatch(string[] strings) =>
                _ => _.QueryText == @"
                              SELECT 
                                  c.content.current.publishedProviderId
                              FROM publishedProvider c
                              WHERE c.documentType = 'PublishedProvider'
                              AND ARRAY_CONTAINS(@publishedProviderIds, c.content.current.publishedProviderId)
                              AND c.deleted = false
                              AND (IS_NULL(c.content.current.errors) OR ARRAY_LENGTH(c.content.current.errors) = 0)" &&
                     HasArrayParameter(_, "@publishedProviderIds", ids);
        }

        private ExpandoObject NewPublishedProviderId(string id)
        {
            ExpandoObject expando = new ExpandoObject();

            IDictionary<string, object> asDictionary = expando;

            asDictionary["publishedProviderId"] = id;

            return expando;
        }

        private void GivenTheDynamicResultsForTheQuery(Func<CosmosDbQuery, bool> queryMatch, IEnumerable<object> expectedResults)
        {
            _cosmosRepository
                .DynamicQuery(Arg.Is<CosmosDbQuery>(_ => queryMatch(_)))
                .Returns(expectedResults);
        }

        private TItem[] AsArray<TItem>(params TItem[] items) => items;

        private async Task<IEnumerable<PublishedProviderFunding>> WhenThePublishedProviderFundingIsQueried(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
            => await _repository.GetPublishedProvidersFunding(publishedProviderIds, specificationId, statuses);

        private async Task<IEnumerable<PublishedProviderFundingSummary>> WhenGetReleaseFundingPublishedProviders(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
            => await _repository.GetReleaseFundingPublishedProviders(publishedProviderIds, specificationId, statuses);

        private async Task<IEnumerable<PublishedProviderFundingCsvData>> WhenThePublishedProviderFundingDataForCsvReportIsQueried(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
            => await _repository.GetPublishedProvidersFundingDataForCsvReport(publishedProviderIds, specificationId, statuses);

        private async Task WhenThePublishedProvidersAreDeleted()
        {
            await _repository.DeleteAllPublishedProvidersByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }
        private async Task WhenThePublishedProviderVersionsAreDeleted()
        {
            await _repository.DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task WhenThePublishedFundingsAreDeleted()
        {
            await _repository.DeleteAllPublishedFundingsByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task WhenThePublishedFundingVersionsAreDeleted()
        {
            await _repository.DeleteAllPublishedFundingVersionsByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task WhenThePublishedProviderVersionsAreRetrieved()
        {
            await _repository.GetPublishedProviderId(_publishedProviderVersion);
        }

        private async Task WhenPublishedGroupBatchProcessing(string specificationId, Func<List<PublishedFunding>, Task> batchProcessor, int batchSize)
        {
            await _repository.PublishedGroupBatchProcessing(specificationId, batchProcessor, batchSize);
        }

        private async Task WhenQueryPublishedProvider(string specificationId,
            IEnumerable<string> fundingIds)
        {
            await _repository.QueryPublishedProvider(specificationId, fundingIds);
        }

        private bool HasParameter(CosmosDbQuery query, string name, string value)
        {
            return query.Parameters?.Any(_ => _.Name == name &&
                                              (string)_.Value == value) == true;
        }
        private bool HasArrayParameter(CosmosDbQuery query, string name, IEnumerable<string> value)
        {
            return query.Parameters?.Any(_ => _.Name == name &&
                                              ((IEnumerable<string>)_.Value).All(value.Contains)) == true;
        }

        private void GivenTheRepositoryServiceHealth(bool expectedIsOkFlag, string expectedMessage)
        {
            _cosmosRepository.IsHealthOk().Returns((expectedIsOkFlag, expectedMessage));
        }

        private string NewRandomString() => new RandomString();
        private JArray AsJArray(params object[] content) => new JArray(content);

        private PublishedProviderStatus NewRandomStatus() => new RandomEnum<PublishedProviderStatus>();

        private dynamic CreatePublishedProviderFundingResult(string specificationId, string publishedProviderId, bool? isIndicative = null)
        {
            string fundingStreamId = NewRandomString();
            string providerType = NewRandomString();
            string providerSubType = NewRandomString();
            string laCode = NewRandomString();
            decimal totalFunding = NewRandomNumber();

            dynamic result = new ExpandoObject();
            result.specificationId = specificationId;
            result.publishedProviderId = publishedProviderId;
            result.fundingStreamId = fundingStreamId;
            result.totalFunding = totalFunding;
            result.providerType = providerType;
            result.providerSubType = providerSubType;
            result.laCode = laCode;
            result.isIndicative = isIndicative;

            return result;
        }

        private dynamic CreateReleaseFundingSummaryPublishedProviderFundingResult(
            string specificationId, string publishedProviderId, bool? isIndicative = null, string status = "Open")
        {
            string providerId = NewRandomString();
            string providerType = NewRandomString();
            string providerSubType = NewRandomString();
            decimal totalFunding = NewRandomNumber();
            int majorVersion = NewRandomNumber();
            int minorVersion = NewRandomNumber();

            dynamic provider = new ExpandoObject();
            provider.providerId = providerId;
            provider.providerType = providerType;
            provider.providerSubType = providerSubType;
            provider.status = status;
            provider.trustStatus = ProviderTrustStatus.NotApplicable;
            provider.name = NewRandomString();
            provider.urn = NewRandomString();
            provider.ukprn = NewRandomString();
            provider.upin = NewRandomString();
            provider.establishmentNumber = NewRandomString();
            provider.furtherEducationTypeCode = NewRandomString();
            provider.furtherEducationTypeName = NewRandomString();
            provider.dfeEstablishmentNumber = NewRandomString();
            provider.authority = NewRandomString();
            provider.dateOpened = DateTime.Now;
            provider.dateClosed = DateTime.Now;
            provider.providerProfileIdType = NewRandomString();
            provider.laCode = NewRandomString();
            provider.navVendorNo = NewRandomString();
            provider.crmAccountId = NewRandomString();
            provider.legalName = NewRandomString();
            provider.phaseOfEducation = NewRandomString();
            provider.reasonEstablishmentOpened = NewRandomString();
            provider.reasonEstablishmentClosed = NewRandomString();
            provider.successor = NewRandomString();
            provider.trustName = NewRandomString();
            provider.trustCode = NewRandomString();
            provider.town = NewRandomString();
            provider.postcode = NewRandomString();
            provider.companiesHouseNumber = NewRandomString();
            provider.groupIdNumber = NewRandomString();
            provider.rscRegionName = NewRandomString();
            provider.rscRegionCode = NewRandomString();
            provider.governmentOfficeRegionName = NewRandomString();
            provider.governmentOfficeRegionCode = NewRandomString();
            provider.districtName = NewRandomString();
            provider.districtCode = NewRandomString();
            provider.wardName = NewRandomString();
            provider.wardCode = NewRandomString();
            provider.censusWardName = NewRandomString();
            provider.censusWardCode = NewRandomString();
            provider.middleSuperOutputAreaName = NewRandomString();
            provider.middleSuperOutputAreaCode = NewRandomString();
            provider.lowerSuperOutputAreaName = NewRandomString();
            provider.lowerSuperOutputAreaCode = NewRandomString();
            provider.parliamentaryConstituencyName = NewRandomString();
            provider.parliamentaryConstituencyCode = NewRandomString();
            provider.londonRegionCode = NewRandomString();
            provider.londonRegionName = NewRandomString();
            provider.countryCode = NewRandomString();
            provider.countryName = NewRandomString();
            provider.localGovernmentGroupTypeCode = NewRandomString();
            provider.localGovernmentGroupTypeName = NewRandomString();
            provider.paymentOrganisationIdentifier = NewRandomString();
            provider.paymentOrganisationName = NewRandomString();

            dynamic result = new ExpandoObject();
            result.specificationId = specificationId;
            result.majorVersion = majorVersion;
            result.minorVersion = minorVersion;
            result.totalFunding = totalFunding;
            result.isIndicative = isIndicative;
            result.provider = provider;
            result.status = status;

            return result;
        }

        private dynamic CreatePublishedProviderFundingCsvData(string specificationid, string providerName, string status, bool hasReleasedVersion = false)
        {
            dynamic data = new ExpandoObject();
            data.specificationId = specificationid;
            data.fundingStreamId = NewRandomString();
            data.fundingPeriodId = NewRandomString();
            data.name = providerName;
            data.ukprn = NewRandomString();
            data.urn = NewRandomString();
            data.upin = NewRandomString();
            data.totalFunding = NewRandomNumber();
            data.status = status;
            data.isIndicative = NewRandomFlag();
            data.majorVersion = NewRandomNumber();
            data.minorVersion = NewRandomNumber();
            data.variationReasons = AsJArray(Array.Empty<string>());
            data.lastReleasedMajorVersion = hasReleasedVersion ? NewRandomNumber() : (int?)null;
            data.lastReleasedMinorVersion = hasReleasedVersion ? NewRandomNumber() : (int?)null;
            data.lastReleasedTotalFunding = hasReleasedVersion ? NewRandomNumber() : (int?)null;

            return data;
        }
    }
}