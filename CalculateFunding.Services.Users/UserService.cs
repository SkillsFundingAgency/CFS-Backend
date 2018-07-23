using CalculateFunding.Models.Health;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users
{
    public class UserService : IUserService, IHealthChecker
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger _logger;
        private readonly ICacheProvider _cacheProvider;

        public UserService(IUserRepository userRepository, ILogger logger, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _userRepository = userRepository;
            _logger = logger;
            _cacheProvider = cacheProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth userRepoHealth = await ((IHealthChecker)_userRepository).IsHealthOk();
            var cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(UserService)
            };
            health.Dependencies.AddRange(userRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });
 
            return health;
        }

        public async Task<IActionResult> GetUserByUsername(HttpRequest request)
        {
            request.Query.TryGetValue("username", out var username);

            string id = username.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.Error("No username was provided to GetUserByUsername");

                return new BadRequestObjectResult("Null or empty username provided");
            }

            User user = await GetUserByUsername(id);

            if (user == null)
            {
                return new NotFoundResult();
            }

            await _cacheProvider.SetAsync($"{CacheKeys.UserByUsername}-{username}", user);

            return new OkObjectResult(user);
        }

        public async Task<IActionResult> ConfirmSkills(HttpRequest request)
        {
            request.Query.TryGetValue("username", out var username);

            string id = username.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.Error("No username was provided to ConfirmSkills");

                return new BadRequestObjectResult("Null or empty username provided");
            }

            User user = await GetUserByUsername(id);

            if (user == null)
            {
                user = new User
                {
                    Username = id,
                    HasConfirmedSkills = false
                };
            }

            if (!user.HasConfirmedSkills)
            {
                user.HasConfirmedSkills = true;

                HttpStatusCode statusCode = await _userRepository.SaveUser(user);

                if (!statusCode.IsSuccess())
                {
                    _logger.Error($"Failed to confirm skills in database with status code: {statusCode.ToString()}");

                    return new InternalServerErrorResult("Failed to confirm skills");
                }

                await _cacheProvider.SetAsync($"{CacheKeys.UserByUsername}-{username}", user);
            }

            return new NoContentResult();
        }

        async Task<User> GetUserByUsername(string username)
        {
            string cacheKey = $"{CacheKeys.UserByUsername}-{username}";

            User user = await _cacheProvider.GetAsync<User>(cacheKey);

            if (user == null)
            {
                user = await _userRepository.GetUserById(username);

                if (user == null)
                {
                    _logger.Warning($"A user for id {username} could not found");

                    return null;
                }
            }

            return user;
        }
    }
}
