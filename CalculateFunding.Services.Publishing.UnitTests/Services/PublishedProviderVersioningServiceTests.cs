using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.UnitTests;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CalculateFunding.Services.Publishing.Services.UnitTests
{
    [TestClass]
    public class PublishedProviderVersioningServiceTests
    {   
        [TestMethod]
        public void CreateVersions_GivenNullPublishedProviderCreateVersionRequests_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProviderCreateVersionRequest> versions = null;

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Func<Task> test = async () => await service.CreateVersions(versions);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void CreateVersions_GivenPublishedProviderCreateVersionRequestsWithOneItemButNoProvider_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProviderCreateVersionRequest> versions = new[]
            {
                new PublishedProviderCreateVersionRequest()
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Func<Task> test = async () => await service.CreateVersions(versions);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void CreateVersions_GivenPublishedProviderCreateVersionRequestsWithOneItemButNoNewversion_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProviderCreateVersionRequest> versions = new[]
            {
                new PublishedProviderCreateVersionRequest
                {
                    PublishedProvider = new PublishedProvider()
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Func<Task> test = async () => await service.CreateVersions(versions);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void CreateVersions_GivenPublishedProviderCreateVersionRequestsButCreatingVersionCausesException_LogsAndThrows()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";

            IEnumerable<PublishedProviderCreateVersionRequest> versions = new[]
            {
                new PublishedProviderCreateVersionRequest
                {
                    PublishedProvider = new PublishedProvider(),
                    NewVersion = new PublishedProviderVersion
                    {
                         ProviderId = providerId,
                         FundingPeriodId = fundingPeriodId,
                         FundingStreamId = fundingStreamId
                    }
                }
            };

            string id = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}-0";

            ILogger logger = CreateLogger();

            IVersionRepository<PublishedProviderVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .When(x => x.CreateVersion(Arg.Any<PublishedProviderVersion>(), Arg.Is((PublishedProviderVersion)null), Arg.Is(string.Empty)))
                .Do(x => { throw new Exception("Failed to create version"); });

            PublishedProviderVersioningService service = CreateVersioningService(logger, versionRepository);

            //Act
            Func<Task> test = async () => await service.CreateVersions(versions);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be("Failed to create version");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to create new version for published provider version id: {id}"));
        }

        [TestMethod]
        public async Task CreateVersion_GivenValidInputWithNoCurrentVersion_EnsuresCallsCreateVersionWithCorrectParameters()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";

            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest = new PublishedProviderCreateVersionRequest
            {
                PublishedProvider = new PublishedProvider(),
                NewVersion = new PublishedProviderVersion
                {
                    ProviderId = providerId,
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId
                }
            };

            IEnumerable<PublishedProviderCreateVersionRequest> versions = new[]
            {
               publishedProviderCreateVersionRequest
            };

            ILogger logger = CreateLogger();

            IVersionRepository<PublishedProviderVersion> versionRepository = CreateVersionRepository();

            PublishedProviderVersioningService service = CreateVersioningService(logger, versionRepository);

            //Act
            IEnumerable<PublishedProvider> result = await service.CreateVersions(versions);

            //Assert
            await
                versionRepository
                    .Received(1)
                    .CreateVersion(
                        Arg.Is(publishedProviderCreateVersionRequest.NewVersion),
                        Arg.Is((PublishedProviderVersion)null),
                        Arg.Is(string.Empty));
        }

        [TestMethod]
        public async Task CreateVersion_GivenValidInputWithCurrentVersion_EnsuresCallsCreateVersionWithCorrectParameters()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";

            string partitionKey = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest = new PublishedProviderCreateVersionRequest
            {
                PublishedProvider = new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        ProviderId = providerId,
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId
                    }
                },
                NewVersion = new PublishedProviderVersion
                {
                    ProviderId = providerId,
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId
                }
            };

            IEnumerable<PublishedProviderCreateVersionRequest> versions = new[]
            {
               publishedProviderCreateVersionRequest
            };

            ILogger logger = CreateLogger();

            IVersionRepository<PublishedProviderVersion> versionRepository = CreateVersionRepository();

            PublishedProviderVersioningService service = CreateVersioningService(logger, versionRepository);

            //Act
            IEnumerable<PublishedProvider> result = await service.CreateVersions(versions);

            //Assert
            await
                versionRepository
                    .Received(1)
                    .CreateVersion(
                        Arg.Is(publishedProviderCreateVersionRequest.NewVersion),
                        Arg.Is<PublishedProviderVersion>(m =>
                            m.ProviderId == providerId &&
                            m.FundingPeriodId == fundingPeriodId &&
                            m.FundingStreamId == fundingStreamId),
                        Arg.Is(partitionKey));
        }

        [TestMethod]
        public async Task CreateVersion_GivenValidInputWithCurrentVersion_ReturnsPublishedProviderWithNewVersion()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";

            string partitionKey = $"publishedprovider_{providerId}_{fundingPeriodId}_{fundingStreamId}";

            PublishedProviderVersion newCreatedVersion = new PublishedProviderVersion
            {
                ProviderId = providerId,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                Version = 1,
                Date = DateTimeOffset.Now,
                PublishStatus = PublishStatus.Approved
            };

            PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest = new PublishedProviderCreateVersionRequest
            {
                PublishedProvider = new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        ProviderId = providerId,
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId
                    }
                },
                NewVersion = new PublishedProviderVersion
                {
                    ProviderId = providerId,
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId
                }
            };

            IEnumerable<PublishedProviderCreateVersionRequest> versions = new[]
            {
               publishedProviderCreateVersionRequest
            };

            ILogger logger = CreateLogger();

            IVersionRepository<PublishedProviderVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<PublishedProviderVersion>(), Arg.Any<PublishedProviderVersion>(), Arg.Any<string>())
                .Returns(newCreatedVersion);

            PublishedProviderVersioningService service = CreateVersioningService(logger, versionRepository);

            //Act
            IEnumerable<PublishedProvider> result = await service.CreateVersions(versions);

            //Assert
            result
                .Should()
                .HaveCount(1);

            result
                .First()
                .Current
                .Should()
                .BeEquivalentTo(newCreatedVersion);
        }

        [TestMethod]
        public void SaveVersions_GivenNullPublishedProviders_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = null;

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Func<Task> test = async () => await service.SaveVersions(publishedProviders);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SaveVersions_GivenPublishedProviderResultsButSavingVersionsCausesExceptions_LogsAndThrows()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion()
                }
            };

            ILogger logger = CreateLogger();

            IVersionRepository<PublishedProviderVersion> versionRepository = CreateVersionRepository();

            versionRepository
                .When(x => x.SaveVersions(Arg.Any<IEnumerable<KeyValuePair<string, PublishedProviderVersion>>>()))
                .Do(x => { throw new Exception("Failed to save versions"); });

            PublishedProviderVersioningService service = CreateVersioningService(logger, versionRepository);

            //Act
            Func<Task> test = async () => await service.SaveVersions(publishedProviders);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be("Failed to save versions");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to save new published provider versions"));
        }

        [TestMethod]
        public async Task SaveVersions_GivenPublishedProviderResults_EnsuresSavesWithCorrectParameters()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";

            string partitionKey = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        ProviderId = providerId,
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId
                    }
                }
            };

            ILogger logger = CreateLogger();

            IVersionRepository<PublishedProviderVersion> versionRepository = CreateVersionRepository();

            PublishedProviderVersioningService service = CreateVersioningService(logger, versionRepository);

            //Act
            await service.SaveVersions(publishedProviders);

            //Assert
            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<KeyValuePair<string, PublishedProviderVersion>>>(m =>
                        m.First().Key == partitionKey &&
                        m.First().Value == publishedProviders.First().Current
                    ));
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenNullPublishedProviders_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = null;

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Action test = () => service.AssemblePublishedProviderCreateVersionRequests(publishedProviders, null, PublishedProviderStatus.Approved);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenNullAuthor_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider()
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Action test = () => service.AssemblePublishedProviderCreateVersionRequests(publishedProviders, null, PublishedProviderStatus.Approved);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenPublishedProvidersButNoCurrentObject_ThrowsArgumentException()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider()
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            Action test = () => service.AssemblePublishedProviderCreateVersionRequests(publishedProviders, new Reference(), PublishedProviderStatus.Approved);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenOnePublishedProvidersButStatusHasntChanged_ReturnsEmptyCreateVersionrequests()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Approved
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(publishedProviders, new Reference(), PublishedProviderStatus.Approved);

            //Assert
            results
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenTwoPublishedProvidersButStatusHasntChangedForOne_ReturnsListWithOneCreateVersionRequest()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Approved
                    }
                },
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Updated
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(publishedProviders, new Reference(), PublishedProviderStatus.Approved);

            //Assert
            results
                .Should()
                .HaveCount(1);
        }
        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenTwoPublishedProvidersButStatusHasntChangedForOne_ReturnsListWithTwoCreatedVersionRequest_WhenForceUpdated()
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Approved
                    }
                },
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Updated
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(publishedProviders, new Reference(), PublishedProviderStatus.Approved, force: true);

            //Assert
            results
                .Should()
                .HaveCount(2);
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenPublishedProviders_EnsuresAssembled()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";

            string partitionKey = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Updated,
                        ProviderId = providerId,
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), PublishedProviderStatus.Approved);

            //Assert
            results
                .Should()
                .HaveCount(1);

            results.First().NewVersion.FundingPeriodId.Should().Be(fundingPeriodId);
            results.First().NewVersion.FundingStreamId.Should().Be(fundingStreamId);
            results.First().NewVersion.ProviderId.Should().Be(providerId);
            results.First().NewVersion.PartitionKey.Should().Be(partitionKey);
            results.First().NewVersion.Status.Should().Be(PublishedProviderStatus.Approved);
            results.First().NewVersion.Author.Id.Should().Be("id1");
            results.First().NewVersion.Author.Name.Should().Be("Joe Bloggs");
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenPublishedProvidersWithJobIdandCorrelationId_PopulatesCreateVersionRequestsWithJobIdandCorrelationId()
        {
            //Arrange
            const string providerId = "123";
            const string fundingPeriodId = "456";
            const string fundingStreamId = "789";
            const string jobId = "JobId-abc-133";
            const string correlationId = "CorrelationId-xyz-123";

            string partitionKey = $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Updated,
                        ProviderId = providerId,
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), PublishedProviderStatus.Approved, jobId, correlationId);

            IEnumerable<PublishedProviderCreateVersionRequest> expectedproviders = new List<PublishedProviderCreateVersionRequest>()
            {
                new PublishedProviderCreateVersionRequest()
                {
                    NewVersion = new PublishedProviderVersion()
                    {
                        FundingPeriodId = fundingPeriodId,
                        FundingStreamId = fundingStreamId,
                        ProviderId = providerId,
                        Status = PublishedProviderStatus.Approved,
                        PublishStatus = PublishStatus.Approved,
                        Author = new Reference()
                        {
                            Id = "id1",
                            Name = "Joe Bloggs"
                        },
                        JobId = jobId,
                        CorrelationId = correlationId                        
                    },
                    PublishedProvider = new PublishedProvider()
                    {
                        Current = new PublishedProviderVersion()
                        {
                            FundingPeriodId = fundingPeriodId,
                            FundingStreamId = fundingStreamId,
                            ProviderId = providerId,
                            Status = PublishedProviderStatus.Updated,
                            PublishStatus = PublishStatus.Draft
                        }
                    }
                }
            };


            //Assert
            results
                .Should()
                .HaveCount(1);


            results
                .Should()
                .BeEquivalentTo(expectedproviders);
            
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenReleasedPublishedProvidersForUpdateRequest_ReturnsCreateVersionRequestWithVersionUpgrade()
        {
            //Arrange
            const int majorVersion = 1;
            const int minorVersion = 0;

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Released,
                        MajorVersion = majorVersion,
                        MinorVersion = minorVersion
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), PublishedProviderStatus.Updated);

            //Assert
            results
                .Should()
                .HaveCount(1);

            results.First().NewVersion.MajorVersion.Should().Be(majorVersion);
            results.First().NewVersion.MinorVersion.Should().Be(minorVersion + 1);
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenApprovedPublishedProvidersForUpdateRequest_ReturnsCreateVersionRequestWithVersionUpgrade()
        {
            //Arrange
            const int majorVersion = 1;
            const int minorVersion = 0;

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = PublishedProviderStatus.Approved,
                        MajorVersion = majorVersion,
                        MinorVersion = minorVersion
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), PublishedProviderStatus.Updated);

            //Assert
            results
                .Should()
                .HaveCount(1);

            results.First().NewVersion.MajorVersion.Should().Be(majorVersion);
            results.First().NewVersion.MinorVersion.Should().Be(minorVersion + 1);
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenPublishedProvidersForReleaseRequest_ReturnsCreateVersionRequestWithVersionUpgrade()
        {
            //Arrange
            const int majorVersion = 1;
            const int minorVersion = 0;

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        MajorVersion = majorVersion,
                        MinorVersion = minorVersion
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), PublishedProviderStatus.Released);

            //Assert
            results
                .Should()
                .HaveCount(1);

            results.First().NewVersion.MajorVersion.Should().Be(majorVersion + 1);
            results.First().NewVersion.MinorVersion.Should().Be(minorVersion);
        }

        [TestMethod]
        public void AssemblePublishedProviderCreateVersionRequests_GivenPublishedProvidersForReleaseRequest_ReturnsCreateVersionRequestWithReleaseVersionSet()
        {
            //Arrange
            const int majorVersion = 1;
            const int minorVersion = 0;

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        MajorVersion = majorVersion,
                        MinorVersion = minorVersion
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), PublishedProviderStatus.Released);

            //Assert
            results
                .Should()
                .HaveCount(1);

            results.First().PublishedProvider.Released.Should().NotBeNull();
        }

        [DataTestMethod]
        [DataRow(PublishedProviderStatus.Approved, PublishedProviderStatus.Released, PublishStatus.Approved)]
        [DataRow(PublishedProviderStatus.Updated, PublishedProviderStatus.Updated, PublishStatus.Updated)]
        [DataRow(PublishedProviderStatus.Draft, PublishedProviderStatus.Draft, PublishStatus.Draft)]
        [DataRow(PublishedProviderStatus.Updated, PublishedProviderStatus.Approved, PublishStatus.Approved)]
        public void AssemblePublishedProviderCreateVersionRequests_GivenPublishedProviders_ReturnsCreateVersionRequestWithPublishStatus(
            PublishedProviderStatus currentStatus, PublishedProviderStatus givenPublishedProviderStatus, PublishStatus expectedPublishStatus)
        {
            //Arrange
            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                new PublishedProvider
                {
                    Current = new PublishedProviderVersion
                    {
                        Status = currentStatus
                    }
                }
            };

            PublishedProviderVersioningService service = CreateVersioningService();

            //Act
            IEnumerable<PublishedProviderCreateVersionRequest> results = service.AssemblePublishedProviderCreateVersionRequests(
                publishedProviders, new Reference("id1", "Joe Bloggs"), givenPublishedProviderStatus);

            //Assert
            results
                .Should()
                .HaveCount(1);

            results.First().NewVersion.PublishStatus.Should().Be(expectedPublishStatus);
        }

        private static PublishedProviderVersioningService CreateVersioningService(
         ILogger logger = null,
         IVersionRepository<PublishedProviderVersion> versionRepository = null)
        {
            IConfiguration configuration = Substitute.For<IConfiguration>();

            return new PublishedProviderVersioningService(
                logger ?? CreateLogger(),
                versionRepository ?? CreateVersionRepository(),
                PublishingResilienceTestHelper.GenerateTestPolicies(),
                new PublishingEngineOptions(configuration));
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IVersionRepository<PublishedProviderVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<PublishedProviderVersion>>();
        }
    }
}
