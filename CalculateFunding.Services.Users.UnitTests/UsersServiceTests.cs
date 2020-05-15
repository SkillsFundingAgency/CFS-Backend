using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Users.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public class UsersServiceTests
    {
        const string UserId = "a7420615-e5d1-4e46-a505-6538d741dbf3";
        const string Username = "sombody@education.gov.uk";
        const string Name = "First Last";


        [TestMethod]
        public async Task GetUserByUserId_GivenUserIdNotProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            UserService userService = CreateUserService(logger: logger);

            //Act
            IActionResult result = await userService.GetUserByUserId(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty userId provided");

            logger
                .Received(1)
                .Error(Arg.Is("No userId was provided to GetUserByUserId"));
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserIdButNotFound_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns((User)null);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.GetUserByUserId(UserId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"A user for id {UserId} could not found"));
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserFoundInCache_ReturnsUserFromCache()
        {
            //Arrange
            User user = new User
            {
                Username = UserId
            };
            
            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.GetUserByUserId(UserId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                userRepository
                    .DidNotReceive()
                    .GetUserById(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserNotFoundInCacheButInDatabase_SetsUserInCacheReturnsUser()
        {
            //Arrange
            User user = new User
            {
                UserId = UserId
            };

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.GetUserByUserId(UserId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Any<String>(), Arg.Is(user));
        }

        [TestMethod]
        public async Task ConfirmSkills_GivenUserIdNotProvided_ReturnsBadRequestObject()
        {
            //Arrange
            ILogger logger = CreateLogger();

            UserService userService = CreateUserService(logger: logger);

            //Act
            IActionResult result = await userService.ConfirmSkills(null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty userId provided");

            logger
                .Received(1)
                .Error(Arg.Is("No userId was provided to ConfirmSkills"));
        }

        [TestMethod]
        public async Task ConfirmSkills_GivenUserIdButNotFound_CreatesUserReturnsOkWithUser()
        {
            //Arrange
            UserCreateModel userCreateModel = new UserCreateModel()
            {
                Name = Name,
                Username = Username
            };

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns((User)null);

            userRepository
                .SaveUser(Arg.Any<User>())
                .Returns(HttpStatusCode.OK);

            IValidator<UserCreateModel> validator = CreateUserCreateModelValidator();
            validator
                .ValidateAsync(Arg.Any<UserCreateModel>())
                .Returns(new ValidationResult());


            UserService userService = CreateUserService(userRepository, logger, cacheProvider, validator);

            //Act
            IActionResult result = await userService.ConfirmSkills(UserId, userCreateModel);

            //Assert
            result
                 .Should()
                 .BeOfType<OkObjectResult>()
                 .Which
                 .Value
                 .Should()
                 .BeOfType<User>();
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserFoundInCacheAndAlreadyConfirmed_ReturnsOkWithUserResult()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = true,
                Name = Name,
                UserId = UserId,
            };

            UserCreateModel userCreateModel = new UserCreateModel()
            {
                Name = Name,
                Username = Username
            };

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();

            IValidator<UserCreateModel> validator = CreateUserCreateModelValidator();
            validator
                .ValidateAsync(Arg.Any<UserCreateModel>())
                .Returns(new ValidationResult());


            UserService userService = CreateUserService(userRepository, logger, cacheProvider, validator);

            //Act
            IActionResult result = await userService.ConfirmSkills(UserId, userCreateModel);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<User>();

            await
                userRepository
                    .DidNotReceive()
                    .GetUserById(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserFoundInCacheButUpdatingReturnsBadRequest_ReturnsInternalServerError()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false,
                Name = Name,
                UserId = UserId,
            };

            UserCreateModel userCreateModel = new UserCreateModel()
            {
                Name = Name,
                Username = Username
            };

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.BadRequest);

            IValidator<UserCreateModel> validator = CreateUserCreateModelValidator();
            validator
                .ValidateAsync(Arg.Any<UserCreateModel>())
                .Returns(new ValidationResult());


            UserService userService = CreateUserService(userRepository, logger, cacheProvider, validator);

            //Act
            IActionResult result = await userService.ConfirmSkills(UserId, userCreateModel);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Failed to confirm skills");

            logger
                .Received(1)
                .Error(Arg.Is("Failed to confirm skills in database with status code: BadRequest"));
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserFoundInCacheButUpdatingIsSuccess_UpdatesCacheReturnsUser()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false,
                Name = Name,
                UserId = UserId,
            };

            UserCreateModel userCreateModel = new UserCreateModel()
            {
                Name = Name,
                Username = Username
            };
            
            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.OK);

            IValidator<UserCreateModel> validator = CreateUserCreateModelValidator();
            validator
                .ValidateAsync(Arg.Any<UserCreateModel>())
                .Returns(new ValidationResult());

            UserService userService = CreateUserService(userRepository, logger, cacheProvider, validator);

            //Act
            IActionResult result = await userService.ConfirmSkills(UserId, userCreateModel);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<User>();

            user
                .HasConfirmedSkills
                .Should()
                .BeTrue();

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Any<String>(), Arg.Is(user));
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenUserNotFoundInCacheAndUpdatingReturnsBadRequest_ReturnsInternalServerError()
        {
            //Arrange
            User user = new User
            {
                UserId = UserId,
                HasConfirmedSkills = false
            };

            UserCreateModel userCreateModel = new UserCreateModel()
            {
                Name = Name,
                Username = Username
            };

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(UserId))
                .Returns(user);

            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.BadRequest);

            IValidator<UserCreateModel> validator = CreateUserCreateModelValidator();
            validator
                .ValidateAsync(Arg.Any<UserCreateModel>())
                .Returns(new ValidationResult());

            UserService userService = CreateUserService(userRepository, logger, cacheProvider, validator);

            //Act
            IActionResult result = await userService.ConfirmSkills(UserId, userCreateModel);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Failed to confirm skills");

            logger
                .Received(1)
                .Error(Arg.Is("Failed to confirm skills in database with status code: BadRequest"));
        }

        [TestMethod]
        public async Task GetUserByUserId_GivenNotUserFoundInCacheAndUpdatingIsSuccess_UpdatesCacheReturnsOkWithUser()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false,
                Name = Name,
                UserId = UserId,
            };

            UserCreateModel userCreateModel = new UserCreateModel()
            {
                Name = Name,
                Username = Username
            };

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
               .GetUserById(Arg.Is(UserId))
               .Returns(user);

            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.OK);

            IValidator<UserCreateModel> validator = CreateUserCreateModelValidator();
            validator
                .ValidateAsync(Arg.Any<UserCreateModel>())
                .Returns(new ValidationResult());

            UserService userService = CreateUserService(userRepository, logger, cacheProvider, validator);

            //Act
            IActionResult result = await userService.ConfirmSkills(UserId, userCreateModel);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<User>();

            user
                .HasConfirmedSkills
                .Should()
                .BeTrue();

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Any<String>(), Arg.Is(user));
        }

        public static UserService CreateUserService(
            IUserRepository userRepository = null,
            ILogger logger = null,
            ICacheProvider cacheProvider = null,
            IValidator<UserCreateModel> userCreateModelValidator = null)
        {
            return new UserService(
                userRepository ?? CreateUserRepository(),
                logger ?? CreateLogger(),
                cacheProvider ?? CreateCacheProvider(),
                userCreateModelValidator ?? CreateUserCreateModelValidator());
        }

        public static IUserRepository CreateUserRepository()
        {
            return Substitute.For<IUserRepository>();
        }

        public static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        public static IValidator<UserCreateModel> CreateUserCreateModelValidator()
        {
            return Substitute.For<IValidator<UserCreateModel>>();
        }
    }
}
