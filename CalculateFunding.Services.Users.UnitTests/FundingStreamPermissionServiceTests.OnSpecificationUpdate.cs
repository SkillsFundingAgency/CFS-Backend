using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Users.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using CalculateFunding.Services.Core;
using CalculateFunding.Models.Messages;


namespace CalculateFunding.Services.Users
{
    public partial class FundingStreamPermissionServiceTests
    {
        [TestMethod]
        public async Task OnSpecificationUpdate_WhenFundingStreamOnSpecificationIsAdded_ThenEffectiveUserPermissionsCleared()
        {
            // Arrange
            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel()
            {
                Current = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                        new Reference("fs2", "Funding Stream 2"),
                    }
                },
                Previous = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                    }
                },
                Id = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(comparisonModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ICacheProvider cacheProvider = CreateCacheProvider();

            IUserRepository userRepository = CreateUserRepository();

            List<FundingStreamPermission> fs1Permissions = new List<FundingStreamPermission>();
            fs1Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user1",
                FundingStreamId = "fs1"
            });

            List<FundingStreamPermission> fs2Permissions = new List<FundingStreamPermission>();
            fs2Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user2",
                FundingStreamId = "fs2"
            });
            fs2Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user3",
                FundingStreamId = "fs2"
            });

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"))
                .Returns(fs1Permissions);

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"))
                .Returns(fs2Permissions);

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(
                userRepository: userRepository,
                cacheProvider: cacheProvider,
                logger: logger);

            // Act
            await service.Process(message);

            // Assert
            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"));

            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user1"), Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user2"), Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user3"), Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Found changed funding streams for specification '{SpecificationId}' Previous: {PreviousFundingStreams} Current {CurrentFundingStreams}"),
                    Arg.Is(SpecificationId),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 1),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 2)
                );

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user1"),
                    Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user2"),
                    Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user3"),
                    Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task OnSpecificationUpdate_WhenFundingStreamOnSpecificationIsRemoved_ThenEffectiveUserPermissionsCleared()
        {
            // Arrange
            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel()
            {
                Current = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                    }
                },
                Previous = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                        new Reference("fs2", "Funding Stream 2"),
                    }
                },
                Id = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(comparisonModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ICacheProvider cacheProvider = CreateCacheProvider();

            IUserRepository userRepository = CreateUserRepository();

            List<FundingStreamPermission> fs1Permissions = new List<FundingStreamPermission>();
            fs1Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user1",
                FundingStreamId = "fs1"
            });

            List<FundingStreamPermission> fs2Permissions = new List<FundingStreamPermission>();
            fs2Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user2",
                FundingStreamId = "fs2"
            });
            fs2Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user3",
                FundingStreamId = "fs2"
            });

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"))
                .Returns(fs1Permissions);

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"))
                .Returns(fs2Permissions);

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(
                userRepository: userRepository,
                cacheProvider: cacheProvider,
                logger: logger);

            // Act
            await service.Process(message);

            // Assert
            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"));

            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user1"), Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user2"), Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user3"), Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Found changed funding streams for specification '{SpecificationId}' Previous: {PreviousFundingStreams} Current {CurrentFundingStreams}"),
                    Arg.Is(SpecificationId),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 2),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 1)
                );

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user1"),
                    Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user2"),
                    Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user3"),
                    Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task OnSpecificationUpdate_WhenFundingStreamOnSpecificationIsAddedAndUserHasPermissionsInBothFundingStreams_ThenEffectiveUserPermissionsCleared()
        {
            // Arrange
            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel()
            {
                Current = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                        new Reference("fs2", "Funding Stream 2"),
                    }
                },
                Previous = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                    }
                },
                Id = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(comparisonModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ICacheProvider cacheProvider = CreateCacheProvider();

            IUserRepository userRepository = CreateUserRepository();

            List<FundingStreamPermission> fs1Permissions = new List<FundingStreamPermission>();
            fs1Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user1",
                FundingStreamId = "fs1"
            });
            fs1Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user2",
                FundingStreamId = "fs1"
            });

            List<FundingStreamPermission> fs2Permissions = new List<FundingStreamPermission>();
            fs2Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user2",
                FundingStreamId = "fs2"
            });
            fs2Permissions.Add(new FundingStreamPermission()
            {
                UserId = "user3",
                FundingStreamId = "fs2"
            });

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"))
                .Returns(fs1Permissions);

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"))
                .Returns(fs2Permissions);

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(
                userRepository: userRepository,
                cacheProvider: cacheProvider,
                logger: logger);

            // Act
            await service.Process(message);

            // Assert
            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"));

            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user1"), Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user2"), Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is($"{CacheKeys.EffectivePermissions}:user3"), Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Found changed funding streams for specification '{SpecificationId}' Previous: {PreviousFundingStreams} Current {CurrentFundingStreams}"),
                    Arg.Is(SpecificationId),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 1),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 2)
                );

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user1"),
                    Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user2"),
                    Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Is("user3"),
                    Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task OnSpecificationUpdate_WhenFundingStreamOnSpecificationIsAddedAndNoUserPermissionsExist_ThenNoEffectiveUserPermissionsCleared()
        {
            // Arrange
            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel()
            {
                Current = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                        new Reference("fs2", "Funding Stream 2"),
                    }
                },
                Previous = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                    }
                },
                Id = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(comparisonModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ICacheProvider cacheProvider = CreateCacheProvider();

            IUserRepository userRepository = CreateUserRepository();

            List<FundingStreamPermission> fs1Permissions = new List<FundingStreamPermission>();

            List<FundingStreamPermission> fs2Permissions = new List<FundingStreamPermission>();

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"))
                .Returns(fs1Permissions);

            userRepository
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"))
                .Returns(fs2Permissions);

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(
                userRepository: userRepository,
                cacheProvider: cacheProvider,
                logger: logger);

            // Act
            await service.Process(message);

            // Assert
            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs1"));

            await userRepository
                .Received(1)
                .GetUsersWithFundingStreamPermissions(Arg.Is("fs2"));

            await cacheProvider
                .Received(0)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is<string>(c => c.StartsWith(CacheKeys.EffectivePermissions)), Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("Found changed funding streams for specification '{SpecificationId}' Previous: {PreviousFundingStreams} Current {CurrentFundingStreams}"),
                    Arg.Is(SpecificationId),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 1),
                    Arg.Is<IEnumerable<string>>(c => c.Count() == 2)
                );

            logger
                .Received(0)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Any<string>(),
                    Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task OnSpecificationUpdate_WhenFundingStreamOnSpecificationHasNotChanged_ThenNoEffectiveUserPermissionsCleared()
        {
            // Arrange
            SpecificationVersionComparisonModel comparisonModel = new SpecificationVersionComparisonModel()
            {
                Current = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                    }
                },
                Previous = new Models.Messages.SpecificationVersion()
                {
                    SpecificationId = SpecificationId,
                    FundingStreams = new List<Reference>()
                    {
                        new Reference("fs1", "Funding Stream 1"),
                    }
                },
                Id = SpecificationId,
            };

            string json = JsonConvert.SerializeObject(comparisonModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ICacheProvider cacheProvider = CreateCacheProvider();

            IUserRepository userRepository = CreateUserRepository();

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(
                userRepository: userRepository,
                cacheProvider: cacheProvider,
                logger: logger);

            // Act
            await service.Process(message);

            // Assert
            await userRepository
                .Received(0)
                .GetUsersWithFundingStreamPermissions(Arg.Any<string>());

            await cacheProvider
                .Received(0)
                .DeleteHashKey<EffectiveSpecificationPermission>(Arg.Is<string>(c => c.StartsWith(CacheKeys.EffectivePermissions)), Arg.Is(SpecificationId));

            logger
                .Received(1)
                .Information(
                    Arg.Is("No funding streams have changed for specification '{SpecificationId}' which require effective permission clearing."),
                    Arg.Is(SpecificationId)
                );

            logger
                .Received(0)
                .Information(
                    Arg.Is("Clearing effective permissions for userId '{UserId}' for specification '{SpecificationId}'"),
                    Arg.Any<string>(),
                    Arg.Is(SpecificationId));
        }


        [TestMethod]
        public void OnSpecificationUpdate_WhenMessageBodyIsEmpty_ThenErrorLogged()
        {
            // Arrange

            string json = string.Empty;

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(logger: logger);

            // Act
            Func<Task> func = async () =>
            {
                await service.Run(message);
            };

            // Assert
            func
                .Should()
                .Throw<InvalidModelException>()
                .Which
                .Message
                .Should()
                .Be("The model for type: SpecificationVersionComparisonModel is invalid with the following errors Null or invalid model provided");

            logger
                .Received(1)
                .Error(Arg.Is("A null versionComparison was provided to users"));
        }

        [TestMethod]
        public void OnSpecificationUpdate_WhenMessageBodyIsNull_ThenErrorLogged()
        {
            // Arrange
            Message message = new Message(null);

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(logger: logger);

            // Act
            Func<Task> func = async () =>
            {
                await service.Process(message);
            };

            // Assert
            func
                .Should()
                .Throw<InvalidModelException>()
                .Which
                .Message
                .Should()
                .Be("The model for type: SpecificationVersionComparisonModel is invalid with the following errors Null or invalid model provided");

            logger
                .Received(1)
                .Error(Arg.Is("A null versionComparison was provided to users"));
        }

        [TestMethod]
        public void OnSpecificationUpdate_WhenSpecificationIdIsEmpty_ThenErrorLogged()
        {
            // Arrange

            string json = JsonConvert.SerializeObject(new SpecificationVersionComparisonModel()
            {
                Current = new Models.Messages.SpecificationVersion(),
                Previous = new Models.Messages.SpecificationVersion(),
            });

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            FundingStreamPermissionService service = CreateService(logger: logger);

            // Act
            Func<Task> func = async () =>
            {
                await service.Process(message);
            };

            // Assert
            func
                .Should()
                .Throw<InvalidModelException>()
                .Which
                .Message
                .Should()
                .Be("The model for type: SpecificationVersionComparisonModel is invalid with the following errors Null or invalid specificationId on model");

            logger
                .Received(1)
                .Error(Arg.Is("A null specificationId was provided to users in model"));
        }
    }
}
