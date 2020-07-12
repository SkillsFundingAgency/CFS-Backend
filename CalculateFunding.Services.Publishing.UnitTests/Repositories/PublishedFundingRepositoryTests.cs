using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
                pageRef);

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

            IEnumerable<PublishedFundingIndex> expectedResults = new PublishedFundingIndex[0];
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
                    pageRef))
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
                                           'localAuthorityName' : c.content.provider.localAuthorityName,
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
                                }} AS Current                                
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
        public async Task QueryPublishedProviderShouldRetrievePublishedProvidersBySpecificationAndFunidngIds()
        {
            const string specificationId = "spec-1";
            IEnumerable<string> fundingIds = new[] { "fid1", "fid2" };

            IEnumerable<PublishedProvider> publishedProviders = await WhenQueryPublishedProvider(specificationId, fundingIds);

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

        private async Task<IEnumerable<PublishedProvider>> WhenQueryPublishedProvider(string specificationId, IEnumerable<string> fundingIds)
        {
            return await _repository.QueryPublishedProvider(specificationId, fundingIds);
        }

        private bool HasParameter(CosmosDbQuery query, string name, string value)
        {
            return query.Parameters?.Any(_ => _.Name == name &&
                                              (string) _.Value == value) == true;
        }
        private bool HasArrayParameter(CosmosDbQuery query, string name, IEnumerable<string> value)
        {
            return query.Parameters?.Any(_ => _.Name == name &&
                                              (IEnumerable<string>)_.Value == value) == true;
        }

        private void GivenTheRepositoryServiceHealth(bool expectedIsOkFlag, string expectedMessage)
        {
            _cosmosRepository.IsHealthOk().Returns((expectedIsOkFlag, expectedMessage));
        }

        private string NewRandomString() => new RandomString();
    }
}