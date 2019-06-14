﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Versioning;
using FluentAssertions;
using Microsoft.Azure.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Core.Services
{
    [TestClass]
    public class VersionRepositoryTests
    {
        const string specificationId = "spec-id";

        [TestMethod]
        public async Task SaveVersion_GivenObject_CallsCreate()
        {
            //Arrange
            TestVersionItem testVersion = new TestVersionItem
            {
                SpecificationId = specificationId,
                Version = 1
            };

            ICosmosRepository cosmosRepository = CreateCosmosRepository();

            VersionRepository<TestVersionItem> versionRepository = new VersionRepository<TestVersionItem>(cosmosRepository);

            //Act
            await versionRepository.SaveVersion(testVersion);

            //Assert
            await
                cosmosRepository
                    .Received(1)
                    .UpsertAsync<TestVersionItem>(Arg.Is<TestVersionItem>(
                            m => m.Id == "spec-id_version_1" &&
                                 m.EntityId == "spec-id"
                        ), enableCrossPartitionQuery: Arg.Any<bool>());
        }

        [TestMethod]
        public async Task CreateVersion_GivenNullCurrentVersion_AssignsDefaultValues()
        {
            //Arrange
            TestVersionItem newVersion = new TestVersionItem();

            ICosmosRepository cosmosRepository = CreateCosmosRepository();

            VersionRepository<TestVersionItem> versionRepository = new VersionRepository<TestVersionItem>(cosmosRepository);

            //Act
            newVersion = await versionRepository.CreateVersion(newVersion);

            //Assert
            newVersion
                .Version
                .Should()
                .Be(1);

            newVersion
                .PublishStatus
                .Should()
                .Be(PublishStatus.Draft);

            newVersion
                .Date
                .Date
                .Should()
                .Be(DateTime.Now.Date.ToLocalTime());
        }

        [TestMethod]
        public async Task CreateVersion_GivenACurrentVersionButUnableToFindExistingVersions_SetsVersionToOne()
        {
            //Arrange
            TestVersionItem newVersion = new TestVersionItem
            {
                SpecificationId = specificationId
            };

            TestVersionItem currentVersion = new TestVersionItem
            {
                SpecificationId = specificationId,
                Version = 1
            };

            IQueryable<TestVersionItem> versions = Enumerable.Empty<TestVersionItem>().AsQueryable<TestVersionItem>();

            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            cosmosRepository
                .Query<TestVersionItem>()
                .Returns(versions);

            VersionRepository<TestVersionItem> versionRepository = new VersionRepository<TestVersionItem>(cosmosRepository);

            //Act
            newVersion = await versionRepository.CreateVersion(newVersion, currentVersion);

            //Assert
            newVersion
                .Version
                .Should()
                .Be(1);

            newVersion
                .PublishStatus
                .Should()
                .Be(PublishStatus.Draft);

            newVersion
                .Date
                .Date
                .Should()
                .Be(DateTime.Now.Date.ToLocalTime());
        }

        [TestMethod]
        public async Task CreateVersion_GivenACurrentVersionIsUpdatedAndGetsApproved_CreatesVersion()
        {
            //Arrange
            TestVersionItem newVersion = new TestVersionItem
            {
                SpecificationId = specificationId,
                PublishStatus = PublishStatus.Approved
            };

            TestVersionItem currentVersion = new TestVersionItem
            {
                SpecificationId = specificationId,
                PublishStatus = PublishStatus.Updated,
                Version = 1
            };

            IQueryable<TestVersionItem> versions = Enumerable.Empty<TestVersionItem>().AsQueryable<TestVersionItem>();

            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            cosmosRepository
                .Query<TestVersionItem>()
                .Returns(versions);

            VersionRepository<TestVersionItem> versionRepository = new VersionRepository<TestVersionItem>(cosmosRepository);

            //Act
            newVersion = await versionRepository.CreateVersion(newVersion, currentVersion);

            //Assert
            newVersion
                .Version
                .Should()
                .Be(1);

            newVersion
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            newVersion
                .Date
                .Date
                .Should()
                .Be(DateTime.Now.Date.ToLocalTime());
        }

        [TestMethod]
        public async Task CreateVersion_GivenACurrentVersionWithVersion1_SetsNewVersionToThreeAndStatusToUpdated()
        {
            //Arrange
            TestVersionItem newVersion = new TestVersionItem
            {
                SpecificationId = specificationId,
                PublishStatus = PublishStatus.Approved
            };

            TestVersionItem currentVersion = new TestVersionItem
            {
                SpecificationId = specificationId,
                Version = 2,
                PublishStatus = PublishStatus.Approved
            };

            dynamic[] maxNumber = new dynamic[] { 2 };

            ICosmosRepository cosmosRepository = CreateCosmosRepository();
            cosmosRepository
                .DynamicQuery<dynamic>(Arg.Any<SqlQuerySpec>())
                .Returns(maxNumber.AsQueryable());

            VersionRepository<TestVersionItem> versionRepository = new VersionRepository<TestVersionItem>(cosmosRepository);

            //Act
            newVersion = await versionRepository.CreateVersion(newVersion, currentVersion);

            //Assert
            newVersion
                .Version
                .Should()
                .Be(3);

            newVersion
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            newVersion
                .Date
                .Date
                .Should()
                .Be(DateTime.Now.Date.ToLocalTime());
        }

        static ICosmosRepository CreateCosmosRepository()
        {
            return Substitute.For<ICosmosRepository>();
        }

        private class TestVersionItem : VersionedItem
        {
            public override string Id
            {
                get { return $"{SpecificationId}_version_{Version}"; }
            }

            public override string EntityId
            {
                get { return $"{SpecificationId}"; }
            }

            public string SpecificationId { get; set; }

            public override VersionedItem Clone()
            {
                throw new NotImplementedException();
            }
        }
    }
}
