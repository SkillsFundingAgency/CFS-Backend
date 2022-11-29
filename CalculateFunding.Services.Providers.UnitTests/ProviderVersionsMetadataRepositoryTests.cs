using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionsMetadataRepositoryTests
    {
        private Mock<ICosmosRepository> _cosmos;
        private ProviderVersionsMetadataRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _cosmos = new Mock<ICosmosRepository>();

            _repository = new ProviderVersionsMetadataRepository(_cosmos.Object);
        }

        [TestMethod]
        public async Task IsHealthOkDelegatesToCosmos()
        {
            bool expectedOk = NewRandomFlag();
            string expectedMessage = NewRandomString();

            GivenTheCosmosHealth(expectedOk, expectedMessage);

            ServiceHealth serviceHealth = await WhenTheServiceHealthIsQueried();

            serviceHealth.Dependencies.Count
                .Should()
                .Be(1);

            DependencyHealth health = serviceHealth.Dependencies.Single();

            health
                .Should()
                .BeEquivalentTo(new DependencyHealth
                {
                    HealthOk = expectedOk,
                    Message = expectedMessage
                },
                    opt
                        => opt.Excluding(_ => _.DependencyName));
        }

        [TestMethod]
        public void UpsertProviderVersionByDateGuardsAgainstMissingProviderVersion()
        {
            Func<Task> invocation = () => WhenTheProviderVersionByDateIsUpserted(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>();

            AndNothingWasUpsertedToCosmos<ProviderVersionByDate>();
        }

        [TestMethod]
        public async Task UpsertProviderVersionByDateSetsIdWithDateComponentsAndThenDelegatesToCosmos()
        {
            ProviderVersionByDate providerVersionByDate = NewProviderVersionByDate();
            HttpStatusCode expectedStatusCode = NewRandomStatusCode();

            GivenTheCosmosStatusCodeForTheUpsert(providerVersionByDate, expectedStatusCode);

            HttpStatusCode actualStatusCode = await WhenTheProviderVersionByDateIsUpserted(providerVersionByDate);

            actualStatusCode
                .Should()
                .Be(expectedStatusCode);

            providerVersionByDate.Id
                .Should()
                .Be($"{providerVersionByDate.Year}{providerVersionByDate.Month:00}{providerVersionByDate.Day:00}");
        }

        [TestMethod]
        public void UpsertMasterGuardsAgainstMissingViewModel()
        {
            Func<Task<HttpStatusCode>> invocation = () => WhenTheMasterProviderVersionIsUpserted(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>();

            AndNothingWasUpsertedToCosmos<MasterProviderVersion>();
        }

        [TestMethod]
        public async Task UpsertMasterDelegatesToCosmos()
        {
            MasterProviderVersion masterProviderVersion = NewMasterProviderVersion();
            HttpStatusCode expectedStatusCode = NewRandomStatusCode();

            GivenTheCosmosStatusCodeForTheUpsert(masterProviderVersion, expectedStatusCode);

            HttpStatusCode actualStatusCode = await WhenTheMasterProviderVersionIsUpserted(masterProviderVersion);

            actualStatusCode
                .Should()
                .Be(expectedStatusCode);
        }

        [TestMethod]
        public async Task CreateProviderVersionDelegatesToCosmos()
        {
            ProviderVersionMetadata providerVersion = NewProviderVersionMetadata();
            HttpStatusCode expectedStatusCode = NewRandomStatusCode();

            GivenTheStatusCodeForTheCreate(providerVersion, expectedStatusCode);

            HttpStatusCode actualStatusCode = await WhenTheProviderVersionIsCreated(providerVersion);

            actualStatusCode
                .Should()
                .Be(expectedStatusCode);
        }

        [TestMethod]
        public void CreateProviderVersionGuardsAgainstMissingProviderVersion()
        {
            Func<Task<HttpStatusCode>> invocation = () => WhenTheProviderVersionIsCreated(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>();

            AndNothingWasCreatedInCosmos<ProviderVersionMetadata>();
        }

        [TestMethod]
        public async Task GetMasterProviderVersionReturnsTheFirstMasterProviderVersionItLocatesWithAnIdOfMaster()
        {
            string master = nameof(master);

            MasterProviderVersion masterProviderVersionOne = NewMasterProviderVersion();
            MasterProviderVersion masterProviderVersionTwo = NewMasterProviderVersion(_ => _.WithId(master));
            MasterProviderVersion masterProviderVersionThree = NewMasterProviderVersion();
            MasterProviderVersion masterProviderVersionFive = NewMasterProviderVersion(_ => _.WithId(master));

            GivenTheCosmosContents(masterProviderVersionOne,
                masterProviderVersionTwo,
                masterProviderVersionThree,
                masterProviderVersionFive);

            MasterProviderVersion actualMasterProviderVersion = await WhenTheMasterProviderVersionIsQueried();

            actualMasterProviderVersion
                .Should()
                .BeSameAs(masterProviderVersionTwo);
        }

        [TestMethod]
        public async Task GetProviderVersionByDateReturnsFirstProviderVersionWithMatchingDateComponents()
        {
            DateTime date = NewRandomDate();

            int year = date.Year;
            int month = date.Month;
            int day = date.Day;

            string id = $"{year}{month:00}{day:00}";

            ProviderVersionByDate providerVersionByDateOne = NewProviderVersionByDate();
            ProviderVersionByDate providerVersionByDateTwo = NewProviderVersionByDate(_ => _.WithId(id));
            ProviderVersionByDate providerVersionByDateThree = NewProviderVersionByDate(_ => _.WithId(id));

            GivenTheCosmosContents(providerVersionByDateOne,
                providerVersionByDateTwo,
                providerVersionByDateThree);

            ProviderVersionByDate actualProviderVersionByDate = await WhenTheProviderVersionByDateIsQueried(day, month, year);

            actualProviderVersionByDate
                .Should()
                .BeSameAs(providerVersionByDateTwo);
        }

        [TestMethod]
        public async Task GetProviderVersionsReturnsAllProviderVersionMetadataInTheSuppliedFundingStream()
        {
            string fundingStream = NewRandomString();

            ProviderVersionMetadata providerVersionOne = NewProviderVersionMetadata(_ => _.WithFundingStream(fundingStream));
            ProviderVersionMetadata providerVersionTwo = NewProviderVersionMetadata(_ => _.WithFundingStream(fundingStream));
            ProviderVersionMetadata providerVersionThree = NewProviderVersionMetadata();
            ProviderVersionMetadata providerVersionFour = NewProviderVersionMetadata(_ => _.WithFundingStream(fundingStream));
            ProviderVersionMetadata providerVersionFive = NewProviderVersionMetadata();

            GivenTheCosmosContents(providerVersionOne,
                providerVersionTwo,
                providerVersionThree,
                providerVersionFour,
                providerVersionFive);

            IEnumerable<ProviderVersionMetadata> actualProviderVersions = await WhenTheProviderVersionsAreQueried(fundingStream);

            actualProviderVersions
                .Should()
                .BeEquivalentTo(providerVersionOne, providerVersionTwo, providerVersionFour);
        }

        [TestMethod]
        [DataRow("name", 99, "type", "fundingStream", true)]
        [DataRow("NOT THE name", 99, "type", "fundingStream", false)]
        [DataRow("name", 101, "type", "fundingStream", false)]
        [DataRow("name", 99, "NOT THE type", "fundingStream", false)]
        [DataRow("name", 99, "type", "NOT THE fundingStream", false)]
        public async Task ExistsMatchesAnyByFundingStreamVersionTypeAndName(string queriedName,
            int queriedVersion,
            string queriedType,
            string queriedFundingStream,
            bool expectedExists)
        {
            string type = nameof(type);
            string name = nameof(name);
            string fundingStream = nameof(fundingStream);
            int version = 99;

            ProviderVersion providerVersionOne = NewProviderVersion();
            ProviderVersion providerVersionTwo = NewProviderVersion(_ => _.WithName(name)
                .WithFundingStream(fundingStream)
                .WithVersion(version)
                .WithType(type));

            GivenTheCosmosContents(providerVersionOne,
                providerVersionTwo);

            bool actualExists = await WhenTheProviderVersionExistsIsChecked(queriedName, queriedType, queriedFundingStream, queriedVersion);

            actualExists
                .Should()
                .Be(expectedExists);
        }

        [TestMethod]
        public void GetCurrentProviderVersionGuardsAgainstMissingFundingStreamId()
        {
            Func<Task<CurrentProviderVersion>> invocation = () => WhenTheCurrentProviderVersionIsQueried(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GetCurrentProviderVersionMatchesByTheFundingStreamId()
        {
            string fundingStreamId = NewRandomString();

            CurrentProviderVersion currentProviderVersionOne = NewCurrentProviderVersion();
            CurrentProviderVersion currentProviderVersionTwo = NewCurrentProviderVersion();
            CurrentProviderVersion currentProviderVersionThree = NewCurrentProviderVersion(_ => _.ForFundingStreamId(fundingStreamId));
            CurrentProviderVersion currentProviderVersionFour = NewCurrentProviderVersion();

            GivenTheCosmosContents(currentProviderVersionOne,
                currentProviderVersionTwo,
                currentProviderVersionThree,
                currentProviderVersionFour);

            CurrentProviderVersion actualCurrentProviderVersion = await WhenTheCurrentProviderVersionIsQueried(fundingStreamId);

            actualCurrentProviderVersion
                .Should()
                .BeSameAs(currentProviderVersionThree);
        }

        [TestMethod]
        public void UpsertCurrentProviderVersionGuardsAgainstMissingCurrentProviderVersion()
        {
            Func<Task<HttpStatusCode>> invocation = () => WhenTheCurrentProviderVersionIsUpserted(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>();

            AndNothingWasUpsertedToCosmos<CurrentProviderVersion>();
        }

        [TestMethod]
        public async Task UpsertCurrentProviderVersionDelegatesToCosmos()
        {
            CurrentProviderVersion currentProviderVersion = NewCurrentProviderVersion();
            HttpStatusCode expectedStatusCode = NewRandomStatusCode();

            GivenTheCosmosStatusCodeForTheUpsert(currentProviderVersion, expectedStatusCode);

            HttpStatusCode actualStatusCode = await WhenTheCurrentProviderVersionIsUpserted(currentProviderVersion);

            actualStatusCode
                .Should()
                .Be(expectedStatusCode);
        }

        [TestMethod]
        public async Task GetAllCurrentProviderVersionsReturnAllCurrentProviderVersions()
        {
            CurrentProviderVersion[] expectedCurrentProviderVersions = new[]
            {
                NewCurrentProviderVersion(),
                NewCurrentProviderVersion()
            };

            GivenTheCosmosContents(expectedCurrentProviderVersions);
            IEnumerable<CurrentProviderVersion> currentProviderVersions = await WhenGetAllCurrentProviderVersions();

            currentProviderVersions
                .Should()
                .BeEquivalentTo(expectedCurrentProviderVersions);
        }

        private async Task<IEnumerable<CurrentProviderVersion>> WhenGetAllCurrentProviderVersions()
            => await _repository.GetAllCurrentProviderVersions();

        private async Task<HttpStatusCode> WhenTheCurrentProviderVersionIsUpserted(CurrentProviderVersion currentProviderVersion)
            => await _repository.UpsertCurrentProviderVersion(currentProviderVersion);

        private async Task<CurrentProviderVersion> WhenTheCurrentProviderVersionIsQueried(string fundingStreamId)
            => await _repository.GetCurrentProviderVersion(fundingStreamId);

        private async Task<ServiceHealth> WhenTheServiceHealthIsQueried()
            => await _repository.IsHealthOk();

        private void GivenTheCosmosHealth(bool ok,
            string message)
        {
            _cosmos.Setup(_ => _.IsHealthOk())
                .Returns((ok, message));
        }

        private async Task<HttpStatusCode> WhenTheProviderVersionByDateIsUpserted(ProviderVersionByDate providerVersion)
            => await _repository.UpsertProviderVersionByDate(providerVersion);

        private void AndNothingWasUpsertedToCosmos<TEntity>()
            where TEntity : IIdentifiable
            => _cosmos.Verify(_ => _.UpsertAsync(It.IsAny<TEntity>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()),
                Times.Never);

        private void AndNothingWasCreatedInCosmos<TEntity>()
            where TEntity : IIdentifiable
            => _cosmos.Verify(_ => _.CreateAsync(It.IsAny<TEntity>(),
                    null),
                Times.Never);

        private void GivenTheCosmosStatusCodeForTheUpsert<TEntity>(TEntity entity,
            HttpStatusCode httpStatusCode)
            where TEntity : IIdentifiable
            => _cosmos.Setup(_ => _.UpsertAsync(entity,
                    null,
                    false,
                    true,
                    null))
                .ReturnsAsync(httpStatusCode);

        private void GivenTheStatusCodeForTheCreate<TEntity>(TEntity entity,
            HttpStatusCode httpStatusCode)
            where TEntity : IIdentifiable
            => _cosmos.Setup(_ => _.CreateAsync(entity,
                    null))
                .ReturnsAsync(httpStatusCode);

        private async Task<HttpStatusCode> WhenTheMasterProviderVersionIsUpserted(MasterProviderVersion providerVersion)
            => await _repository.UpsertMaster(providerVersion);

        private async Task<HttpStatusCode> WhenTheProviderVersionIsCreated(ProviderVersionMetadata providerVersion)
            => await _repository.CreateProviderVersion(providerVersion);

        private void GivenTheCosmosContents<TEntity>(params TEntity[] contents)
            where TEntity : IIdentifiable
        {
            ICollection<DocumentEntity<TEntity>> filteredResults = new List<DocumentEntity<TEntity>>();

            _cosmos.Setup(_ => _.GetAllDocumentsAsync(It.IsAny<int>(),
                    It.IsAny<Expression<Func<DocumentEntity<TEntity>, bool>>>()))
                .Callback<int, Expression<Func<DocumentEntity<TEntity>, bool>>>((_,
                    query) =>
                {
                    IEnumerable<DocumentEntity<TEntity>> documents = contents.Select(document => new DocumentEntity<TEntity>(document));

                    if (query != null)
                    {
                        Func<DocumentEntity<TEntity>, bool> filter = query.Compile();

                        filteredResults.AddRange(documents.Where(document => filter(document)));
                    }
                    else
                    {
                        filteredResults.AddRange(documents);
                    }
                })
                .ReturnsAsync(filteredResults);
        }

        private async Task<bool> WhenTheProviderVersionExistsIsChecked(string name,
            string type,
            string fundingStream,
            int version)
            => await _repository.Exists(name, type, version, fundingStream);

        private async Task<IEnumerable<ProviderVersionMetadata>> WhenTheProviderVersionsAreQueried(string fundingStream)
            => await _repository.GetProviderVersions(fundingStream);

        private async Task<ProviderVersionByDate> WhenTheProviderVersionByDateIsQueried(int day,
            int month,
            int year)
            => await _repository.GetProviderVersionByDate(year, month, day);

        private async Task<MasterProviderVersion> WhenTheMasterProviderVersionIsQueried()
            => await _repository.GetMasterProviderVersion();

        private ProviderVersionByDate NewProviderVersionByDate(Action<ProviderVersionByDateBuilder> setUp = null)
        {
            ProviderVersionByDateBuilder providerVersionByDateBuilder = new ProviderVersionByDateBuilder();

            setUp?.Invoke(providerVersionByDateBuilder);

            return providerVersionByDateBuilder.Build();
        }

        private MasterProviderVersion NewMasterProviderVersion(Action<MasterProviderVersionBuilder> setUp = null)
        {
            MasterProviderVersionBuilder masterProviderVersionBuilder = new MasterProviderVersionBuilder();

            setUp?.Invoke(masterProviderVersionBuilder);

            return masterProviderVersionBuilder.Build();
        }

        private ProviderVersionMetadata NewProviderVersionMetadata(Action<ProviderVersionMetadataBuilder> setUp = null)
        {
            ProviderVersionMetadataBuilder providerVersionMetadataBuilder = new ProviderVersionMetadataBuilder();

            setUp?.Invoke(providerVersionMetadataBuilder);

            return providerVersionMetadataBuilder.Build();
        }

        private ProviderVersion NewProviderVersion(Action<ProviderVersionBuilder> setUp = null)
        {
            ProviderVersionBuilder providerVersionBuilder = new ProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        private CurrentProviderVersion NewCurrentProviderVersion(Action<CurrentProviderVersionBuilder> setUp = null)
        {
            CurrentProviderVersionBuilder currentProviderVersionBuilder = new CurrentProviderVersionBuilder();

            setUp?.Invoke(currentProviderVersionBuilder);

            return currentProviderVersionBuilder.Build();
        }

        private bool NewRandomFlag() => new RandomBoolean();

        private string NewRandomString() => new RandomString();

        private DateTime NewRandomDate() => new RandomDateTime();

        private HttpStatusCode NewRandomStatusCode() => (HttpStatusCode)(int)new RandomNumberBetween(200, 500);
    }
}