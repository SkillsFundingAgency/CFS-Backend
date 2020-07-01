using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Specs.UnitTests.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class SpecificationIndexerTests
    {
        private Mock<IDatasetsApiClient> _datasets;
        private Mock<ISearchRepository<SpecificationIndex>> _specifications;
        private Mock<IMapper> _mapper;

        private SpecificationIndexer _indexer;

        [TestInitialize]
        public void SetUp()
        {
            _datasets = new Mock<IDatasetsApiClient>();
            _specifications = new Mock<ISearchRepository<SpecificationIndex>>();
            _mapper = new Mock<IMapper>();

            _indexer = new SpecificationIndexer(_mapper.Object,
                new SpecificationsResiliencePolicies
                {
                    DatasetsApiClient = Policy.NoOpAsync(),
                    SpecificationsSearchRepository = Policy.NoOpAsync()
                },
                _datasets.Object,
                _specifications.Object,
                new ProducerConsumerFactory(),
                Logger.None);
        }

        [TestMethod]
        public async Task MapsSuppliedSearchModelsIntoIndicesAddsDatasetSummaryInformationAndThenIndexesThem()
        {
            DateTimeOffset lastUpdateFour = NewRandomDate();
            DateTimeOffset lastUpdateTwo = lastUpdateFour.AddDays(1);
            DateTimeOffset lastUpdateOne = lastUpdateFour.AddDays(10);

            SpecificationSearchModel[] specifications = AsArray(
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel(),
                NewSpecificationSearchModel()
            );
            SpecificationIndex specificationIndexOne = NewSpecificationIndex();
            SpecificationIndex specificationIndexTwo = NewSpecificationIndex();
            SpecificationIndex specificationIndexThree = NewSpecificationIndex();
            SpecificationIndex specificationIndexFour = NewSpecificationIndex();
            SpecificationIndex specificationIndexFive = NewSpecificationIndex();
            SpecificationIndex specificationIndexSix = NewSpecificationIndex();
            SpecificationIndex specificationIndexSeven = NewSpecificationIndex();

            GivenTheSpecificationIndexMappings(specifications,
                specificationIndexOne,
                specificationIndexTwo,
                specificationIndexThree,
                specificationIndexFour,
                specificationIndexFive,
                specificationIndexSix,
                specificationIndexSeven);
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexFour.Id,
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateFour)),
                NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexSix.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexSeven.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexFive.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexTwo.Id,
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())),
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateTwo)),
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateTwo.AddDays(-1))),
                NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexOne.Id,
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateOne)),
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateOne.AddDays(-1))),
                NewDatasetSpecificationRelationshipViewModel());

            await WhenTheSpecificationsAreIndexed(specifications);

            ThenTheIndicesWereIndexed(specificationIndexOne,
                specificationIndexTwo,
                specificationIndexThree,
                specificationIndexFour,
                specificationIndexFive);
            AndTheIndicesWereIndexed(specificationIndexSix,
                specificationIndexSeven);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexOne, 2, lastUpdateOne);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexTwo, 3, lastUpdateTwo);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexThree, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexFour, 1, lastUpdateFour);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexFive, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexSix, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexSeven, 0, null);
        }

        [TestMethod]
        public async Task MapsSuppliedSpecificationsIntoIndicesAddsDatasetSummaryInformationAndThenAndThenIndexesThem()
        {
            DateTimeOffset lastUpdateFour = NewRandomDate();
            DateTimeOffset lastUpdateTwo = lastUpdateFour.AddDays(1);

            Specification[] specifications = AsArray(
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification(),
                NewSpecification()
            );
            SpecificationIndex specificationIndexOne = NewSpecificationIndex();
            SpecificationIndex specificationIndexTwo = NewSpecificationIndex();
            SpecificationIndex specificationIndexThree = NewSpecificationIndex();
            SpecificationIndex specificationIndexFour = NewSpecificationIndex();
            SpecificationIndex specificationIndexFive = NewSpecificationIndex();
            SpecificationIndex specificationIndexSix = NewSpecificationIndex();
            SpecificationIndex specificationIndexSeven = NewSpecificationIndex();
            SpecificationIndex specificationIndexEight = NewSpecificationIndex();

            GivenTheSpecificationIndexMappings(specifications,
                specificationIndexOne,
                specificationIndexTwo,
                specificationIndexThree,
                specificationIndexFour,
                specificationIndexFive,
                specificationIndexSix,
                specificationIndexSeven,
                specificationIndexEight);
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexFour.Id,
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateFour)),
                NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexSix.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexSeven.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexFive.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexTwo.Id,
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())),
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateTwo)),
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateTwo.AddDays(-1))),
                NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexOne.Id, NewDatasetSpecificationRelationshipViewModel());
            AndTheDatasetRelationshipsForSpecificationId(specificationIndexEight.Id, NewDatasetSpecificationRelationshipViewModel());

            await WhenTheSpecificationsAreIndexed(specifications);

            ThenTheIndicesWereIndexed(specificationIndexOne,
                specificationIndexTwo,
                specificationIndexThree,
                specificationIndexFour,
                specificationIndexFive);
            AndTheIndicesWereIndexed(specificationIndexSix,
                specificationIndexSeven,
                specificationIndexEight);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexOne, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexTwo, 3, lastUpdateTwo);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexThree, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexFour, 1, lastUpdateFour);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexFive, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexSix, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexSeven, 0, null);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndexEight, 0, null);
        }

        [TestMethod]
        public async Task MapsSuppliedSpecificationIntoIndexAddsDatasetSummaryInformationAndThenAndThenIndexesIt()
        {
            DateTimeOffset lastUpdateOne = NewRandomDate();
            DateTimeOffset lastUpdateTwo = lastUpdateOne.AddDays(1);

            Specification specification = NewSpecification();
            SpecificationIndex specificationIndex = NewSpecificationIndex();
            DatasetSpecificationRelationshipViewModel[] relationships = AsArray(NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateTwo)),
                NewDatasetSpecificationRelationshipViewModel(),
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithDatasetId(NewRandomString())
                    .WithLastUpdatedDate(lastUpdateOne)));

            GivenTheSpecificationIndexMappings(AsArray(specification), specificationIndex);
            AndTheDatasetRelationshipsForSpecificationId(specificationIndex.Id, relationships);

            await WhenTheSpecificationIsIndexed(specification);

            ThenTheIndicesWereIndexed(specificationIndex);
            AndTheSpecificationIndexHasTheDatasetsSummaryInformation(specificationIndex, 2, lastUpdateTwo);
        }

        [TestMethod]
        public void CollectsAnyIndexingErrorsAndThrowsThemInNewFailedToIndexSearchException()
        {
            IEnumerable<SpecificationSearchModel> specifications = AsArray(NewSpecificationSearchModel());
            SpecificationIndex[] indicesPageOne = AsArray(NewSpecificationIndex());

            string indexErrorMessageOne = NewRandomString();
            string indexErrorMessageTwo = NewRandomString();

            GivenTheSpecificationIndexMappings(specifications, indicesPageOne);
            AndTheIndexErrors(indicesPageOne,
                NewIndexError(_ => _.WithMessage(indexErrorMessageOne)),
                NewIndexError(_ => _.WithMessage(indexErrorMessageTwo)));

            Func<Task> invocation = () => WhenTheSpecificationsAreIndexed(specifications);

            invocation
                .Should()
                .Throw<FailedToIndexSearchException>()
                .Which
                .Message
                .Should()
                .EndWith($"failed to index search with the following errors: {indexErrorMessageOne};{indexErrorMessageTwo}");
        }

        private async Task WhenTheSpecificationIsIndexed(Specification specification)
        {
            await _indexer.Index(specification);
        }

        private async Task WhenTheSpecificationsAreIndexed(IEnumerable<SpecificationSearchModel> specifications)
        {
            await _indexer.Index(specifications);
        }

        private async Task WhenTheSpecificationsAreIndexed(IEnumerable<Specification> specifications)
        {
            await _indexer.Index(specifications);
        }

        private void AndTheSpecificationIndexHasTheDatasetsSummaryInformation(SpecificationIndex specificationIndex,
            int expectedTotalMappedCount,
            DateTimeOffset? expectedLastDatasetUpdatedDate)
        {
            specificationIndex.TotalMappedDataSets
                .Should()
                .Be(expectedTotalMappedCount);

            specificationIndex.MapDatasetLastUpdated
                .Should()
                .Be(expectedLastDatasetUpdatedDate);
        }

        private void ThenTheIndicesWereIndexed(params SpecificationIndex[] indices)
        {
            _specifications.Verify(_ => _.Index(It.Is<IEnumerable<SpecificationIndex>>(ind =>
                    ind.SequenceEqual(indices))),
                Times.Once);
        }

        private void AndTheIndicesWereIndexed(params SpecificationIndex[] indices)
        {
            ThenTheIndicesWereIndexed(indices);
        }

        private DatasetSpecificationRelationshipViewModel NewDatasetSpecificationRelationshipViewModel(Action<DatasetSpecificationRelationshipViewModelBuilder> setUp = null)
        {
            DatasetSpecificationRelationshipViewModelBuilder datasetSpecificationRelationshipViewModelBuilder = new DatasetSpecificationRelationshipViewModelBuilder();

            setUp?.Invoke(datasetSpecificationRelationshipViewModelBuilder);

            return datasetSpecificationRelationshipViewModelBuilder.Build();
        }

        private void GivenTheSpecificationIndexMappings<TSource>(IEnumerable<TSource> sourceItems,
            params SpecificationIndex[] indices)
        {
            _mapper.Setup(_ => _.Map<IEnumerable<SpecificationIndex>>(sourceItems))
                .Returns(indices);
        }

        private void AndTheDatasetRelationshipsForSpecificationId(string specificationId,
            params DatasetSpecificationRelationshipViewModel[] relationships)
        {
            _datasets.Setup(_ => _.GetRelationshipsBySpecificationId(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationships));
        }

        private void AndTheIndexErrors(IEnumerable<SpecificationIndex> indices,
            params IndexError[] indexErrors)
        {
            _specifications.Setup(_ => _.Index(It.Is<IEnumerable<SpecificationIndex>>(ind => ind.SequenceEqual(indices))))
                .ReturnsAsync(indexErrors.ToList());
        }

        private SpecificationIndex NewSpecificationIndex(Action<SpecificationIndexBuilder> setUp = null)
        {
            SpecificationIndexBuilder specificationIndexBuilder = new SpecificationIndexBuilder();

            setUp?.Invoke(specificationIndexBuilder);

            return specificationIndexBuilder.Build();
        }

        private IndexError NewIndexError(Action<IndexErrorBuilder> setUp = null)
        {
            IndexErrorBuilder indexErrorBuilder = new IndexErrorBuilder();

            setUp?.Invoke(indexErrorBuilder);

            return indexErrorBuilder.Build();
        }

        private SpecificationSearchModel NewSpecificationSearchModel(Action<SpecificationSearchModelBuilder> setUp = null)
        {
            SpecificationSearchModelBuilder specificationSearchModelBuilder = new SpecificationSearchModelBuilder();

            setUp?.Invoke(specificationSearchModelBuilder);

            return specificationSearchModelBuilder.Build();
        }

        private Specification NewSpecification(Action<SpecificationBuilder> setUp = null)
        {
            SpecificationBuilder specificationBuilder = new SpecificationBuilder();

            setUp?.Invoke(specificationBuilder);

            return specificationBuilder.Build();
        }

        private TItem[] AsArray<TItem>(params TItem[] items) => items;

        private static string NewRandomString() => new RandomString();

        private static DateTimeOffset NewRandomDate() => new RandomDateTime();
    }
}