using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Providers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionsMetadataRepositoryTests
    {
        [TestMethod]
        [DataRow(false, "Whoops")]
        [DataRow(false, "Yeah...")]
        [DataRow(true, "Good")]
        [DataRow(true, "Better")]
        public async Task IsHealthOk_ReturnsAsExpected(bool ok, string message)
        {
            //Arrange
            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .IsHealthOk()
                .Returns((ok, message));

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            ServiceHealth serviceHealth = await providerVersionsMetadataRepository.IsHealthOk();

            //Assert
            await repository.Received(1).IsHealthOk();

            serviceHealth.Dependencies.Count.Should().Be(1);

            DependencyHealth health = serviceHealth.Dependencies.ToArray()[0];

            health.HealthOk.Should().Be(ok);
            health.DependencyName.Should().StartWith("ObjectProxy");
            health.Message.Should().Be(message);
        }

        [TestMethod]
        public async Task UpsertProviderVersionByDate_ProviderVersionByDateNull_ThrowsException()
        {
            //Arrange
            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            Func<Task> error = () => providerVersionsMetadataRepository.UpsertProviderVersionByDate(null);

            //Assert
            error.Should().Throw<ArgumentNullException>();

            await repository
                .Received(0)
                .UpsertAsync(Arg.Any<ProviderVersionByDate>());
        }

        [TestMethod]
        public async Task UpsertProviderVersionByDate_ReturnsCorrectly()
        {
            //Arrange
            HttpStatusCode statusCode = HttpStatusCode.Continue;

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .UpsertAsync(Arg.Any<ProviderVersionByDate>())
                .Returns(statusCode);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            ProviderVersionByDate providerVersionByDate = new ProviderVersionByDate
            {
                Description = "A",
                Year = 1,
                Month = 2,
                Day = 3,
                Id = "B"
            };

            //Act
            HttpStatusCode result = await providerVersionsMetadataRepository.UpsertProviderVersionByDate(providerVersionByDate);

            //Assert
            result.Should().Be(statusCode);

            await repository
                .Received(1)
                .UpsertAsync(Arg.Is<ProviderVersionByDate>(x => x.Description == providerVersionByDate.Description
                                                                && x.Id == string.Concat(providerVersionByDate.Year,
                                                                    providerVersionByDate.Month.ToString("00"),
                                                                    providerVersionByDate.Day.ToString("00"))));
        }

        [TestMethod]
        public async Task UpsertMaster_ViewModelNull_ThrowsException()
        {
            //Arrange
            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            Func<Task> error = () => providerVersionsMetadataRepository.UpsertMaster(null);

            //Assert
            error.Should().Throw<ArgumentNullException>();
            await repository
                .Received(0)
                .UpsertAsync(Arg.Any<MasterProviderVersion>());
        }

        [TestMethod]
        public async Task UpsertMaster_ReturnsCorrectly()
        {
            //Arrange
            HttpStatusCode statusCode = HttpStatusCode.EarlyHints;

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .UpsertAsync(Arg.Any<MasterProviderVersion>())
                .Returns(statusCode);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            MasterProviderVersion masterProviderVersion = new MasterProviderVersion();

            //Act
            HttpStatusCode result = await providerVersionsMetadataRepository.UpsertMaster(masterProviderVersion);

            //Assert
            result.Should().Be(statusCode);

            await repository
                .Received(1)
                .UpsertAsync(masterProviderVersion);
        }

        [TestMethod]
        public async Task CreateProviderVersion_MetadataNull_ThrowsException()
        {
            //Arrange
            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            Func<Task> error = () => providerVersionsMetadataRepository.CreateProviderVersion(null);

            //Assert
            error.Should().Throw<ArgumentNullException>();
            await repository
                .Received(0)
                .CreateAsync(Arg.Any<ProviderVersionMetadata>());
        }

        [TestMethod]
        public async Task CreateProviderVersion_ReturnsCorrectly()
        {
            //Arrange
            HttpStatusCode statusCode = HttpStatusCode.EarlyHints;

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .CreateAsync(Arg.Any<ProviderVersionMetadata>())
                .Returns(statusCode);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            ProviderVersionMetadata providerVersionMetadata = new ProviderVersionMetadata();

            //Act
            HttpStatusCode result = await providerVersionsMetadataRepository.CreateProviderVersion(providerVersionMetadata);

            //Assert
            result.Should().Be(statusCode);

            await repository
                .Received(1)
                .CreateAsync(providerVersionMetadata);
        }

        [TestMethod]
        public async Task GetMasterProviderVersion_ReturnsCorrectly()
        {
            MasterProviderVersion mpv1 = new MasterProviderVersion { Id = "1" };
            MasterProviderVersion mpv2 = new MasterProviderVersion { Id = "2" };

            //Arrange
            IEnumerable<DocumentEntity<MasterProviderVersion>> masterProviderVersions = new List<DocumentEntity<MasterProviderVersion>>
            {
                new DocumentEntity<MasterProviderVersion>(mpv1),
                new DocumentEntity<MasterProviderVersion>(mpv2)
            };

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<MasterProviderVersion>, bool>>>())
                .Returns(masterProviderVersions);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            MasterProviderVersion result = await providerVersionsMetadataRepository.GetMasterProviderVersion();

            //Assert
            result.Should().Be(mpv1);
            //TODO test gap around the lambda filter expressions going to cosmos - worth trying this just in case but not surprised it doesn't work
            //await repository
            //    .Received(1)
            //    .GetAllDocumentsAsync(query: Arg.Is<Expression<Func<DocumentEntity<MasterProviderVersion>, bool>>>(x => x.Content.Id == "master"));
            await repository
                .Received(1)
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<MasterProviderVersion>, bool>>>());
        }

        [TestMethod]
        public async Task GetProviderVersionByDate_ReturnsCorrectly()
        {
            ProviderVersionByDate pvbd1 = new ProviderVersionByDate { Id = "1" };
            ProviderVersionByDate pvbd2 = new ProviderVersionByDate { Id = "2" };

            //Arrange
            IEnumerable<DocumentEntity<ProviderVersionByDate>> providerVersionsByDate = new List<DocumentEntity<ProviderVersionByDate>>
            {
                new DocumentEntity<ProviderVersionByDate>(pvbd1),
                new DocumentEntity<ProviderVersionByDate>(pvbd2)
            };

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<ProviderVersionByDate>, bool>>>())
                .Returns(providerVersionsByDate);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            ProviderVersionByDate result = await providerVersionsMetadataRepository.GetProviderVersionByDate(1, 2, 3);

            //Assert
            result.Should().Be(pvbd1);
            //TODO test gap around the lambda filter expressions going to cosmos
            await repository
                .Received(1)
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<ProviderVersionByDate>, bool>>>());
        }

        [TestMethod]
        public async Task GetProviderVersions_ReturnsCorrectly()
        {
            ProviderVersion pv1 = new ProviderVersion { Id = "1" };
            ProviderVersion pv2 = new ProviderVersion { Id = "2" };

            //Arrange
            IEnumerable<DocumentEntity<ProviderVersion>> providerVersions = new List<DocumentEntity<ProviderVersion>>
            {
                new DocumentEntity<ProviderVersion>(pv1),
                new DocumentEntity<ProviderVersion>(pv2)
            };

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<ProviderVersion>, bool>>>())
                .Returns(providerVersions);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            IEnumerable<ProviderVersion> result = await providerVersionsMetadataRepository.GetProviderVersions("fundingStream");

            //Assert
            result
                .Count()
                .Should()
                .Be(providerVersions.Count());

            foreach (ProviderVersion pv in providerVersions.Select(x => x.Content))
            {
                result
                    .Count(x => x.Id == pv.Id)
                    .Should()
                    .Be(1);
            }

            //TODO test gap around the lambda filter expressions going to cosmos
            await repository
                .Received(1)
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<ProviderVersion>, bool>>>());
        }

        [TestMethod]
        [DataRow("Alice", true)]
        [DataRow("Bob", false)]
        public async Task Exists_ReturnsCorrectly(string name, bool exists)
        {
            ProviderVersion pv1 = new ProviderVersion { Id = "1", Name = "Alice" };
            ProviderVersion pv2 = new ProviderVersion { Id = "2", Name = "b" };

            //Arrange
            IEnumerable<DocumentEntity<ProviderVersion>> providerVersions = new List<DocumentEntity<ProviderVersion>>
            {
                new DocumentEntity<ProviderVersion>(pv1),
                new DocumentEntity<ProviderVersion>(pv2)
            };

            ICosmosRepository repository = Substitute.For<ICosmosRepository>();
            repository
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<ProviderVersion>, bool>>>())
                .Returns(providerVersions);

            ProviderVersionsMetadataRepository providerVersionsMetadataRepository = new ProviderVersionsMetadataRepository(repository);

            //Act
            bool result = await providerVersionsMetadataRepository.Exists(name, "", 1, "");

            //Assert
            result
                .Should()
                .Be(exists);

            //TODO test gap around the lambda filter expressions going to cosmos
            await repository
                .Received(1)
                .GetAllDocumentsAsync(query: Arg.Any<Expression<Func<DocumentEntity<ProviderVersion>, bool>>>());
        }
    }
}
