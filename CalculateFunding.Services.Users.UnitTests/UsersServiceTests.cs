using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Users.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public class UsersServiceTests
    {
        const string Username = "whoever@wherever.com";

        [TestMethod]
        public async Task GetUserByUsername_GivenUsernameNotProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            UserService userService = CreateUserService(logger: logger);

            //Act
            IActionResult result = await userService.GetUserByUsername(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty username provided");

            logger
                .Received(1)
                .Error(Arg.Is("No username was provided to GetUserByUsername"));
        }

        [TestMethod]
        public async Task GetUserByUsername_GivenUsernameButNotFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(Username))
                .Returns((User)null);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.GetUserByUsername(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
                
            logger
                .Received(1)
                .Warning(Arg.Is($"A user for id {Username} could not found"));
        }

        [TestMethod]
        public async Task GetUserByUsername_GivenUserFoundInCache_ReturnsUserFromCache()
        {
            //Arrange
            User user = new User
            {
                Username = Username
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.GetUserByUsername(request);

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
        public async Task GetUserByUsername_GivenUserNotFoundInCacheButInDatabase_SetsUserInCacheRetrunsUser()
        {
            //Arrange
            User user = new User
            {
                Username = Username
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(Username))
                .Returns(user);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.GetUserByUsername(request);

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
        public async Task ConfirmSkills_GivenUsernameNotProvided_ReturnsBadRequestObject()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            UserService userService = CreateUserService(logger: logger);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty username provided");

            logger
                .Received(1)
                .Error(Arg.Is("No username was provided to ConfirmSkills"));
        }

        [TestMethod]
        public async Task ConfirmSkills_GivenUsernameButNotFound_CreatesUserReturnsNoContent()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(Username))
                .Returns((User)null);

            userRepository
                .SaveUser(Arg.Any<User>())
                .Returns(HttpStatusCode.OK);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task GetUserByUsername_GivenUserFoundInCacheAndAlreadyConfirmed_ReturnsNoContent()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = true
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await
                userRepository
                    .DidNotReceive()
                    .GetUserById(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetUserByUsername_GivenUserFoundInCacheButUpdatingReturnsBadRequest_ReturnsInternalServerError()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.BadRequest);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

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
        public async Task GetUserByUsername_GivenUserFoundInCacheButUpdatingIsSuccess_UpdatesCacheReturnsNoContent()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns(user);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.OK);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

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
        public async Task GetUserByUsername_GivenUserNotFoundInCacheAndUpdatingReturnsBadRequest_ReturnsInternalServerError()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
                .GetUserById(Arg.Is(Username))
                .Returns(user);

            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.BadRequest);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

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
        public async Task GetUserByUsername_GivenNotUserFoundInCacheAndUpdatingIsSuccess_UpdatesCacheReturnsNoContent()
        {
            //Arrange
            User user = new User
            {
                Username = Username,
                HasConfirmedSkills = false
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "username", new StringValues(Username) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<User>(Arg.Any<string>())
                .Returns((User)null);

            IUserRepository userRepository = CreateUserRepository();
            userRepository
               .GetUserById(Arg.Is(Username))
               .Returns(user);

            userRepository
                .SaveUser(Arg.Is(user))
                .Returns(HttpStatusCode.OK);

            UserService userService = CreateUserService(userRepository, logger, cacheProvider);

            //Act
            IActionResult result = await userService.ConfirmSkills(request);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            user
                .HasConfirmedSkills
                .Should()
                .BeTrue();

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Any<String>(), Arg.Is(user));
        }

        public static UserService CreateUserService(IUserRepository userRepository = null, 
            ILogger logger = null, ICacheProvider cacheProvider = null)
        {
            return new UserService(userRepository ?? CreateUserRepository(), logger ?? CreateLogger(), cacheProvider ?? CreateCacheProvider());
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
    }
}
