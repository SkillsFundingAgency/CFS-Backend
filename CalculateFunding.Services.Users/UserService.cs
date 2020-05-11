using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Users.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CalculateFunding.Services.Users
{
    public class UserService : IUserService, IHealthChecker
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger _logger;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<UserCreateModel> _userCreateModelValidator;

        public UserService(
            IUserRepository userRepository,
            ILogger logger,
            ICacheProvider cacheProvider,
            IValidator<UserCreateModel> userCreateModelValidator)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(userCreateModelValidator, nameof(userCreateModelValidator));

            _userRepository = userRepository;
            _logger = logger;
            _cacheProvider = cacheProvider;
            _userCreateModelValidator = userCreateModelValidator;
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

        public async Task<IActionResult> GetUserByUserId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.Error($"No userId was provided to {nameof(GetUserByUserId)}");

                return new BadRequestObjectResult("Null or empty userId provided");
            }

            User user = await GetUserByUserIdInternal(id);

            if (user == null)
            {
                return new NotFoundResult();
            }

            await _cacheProvider.SetAsync($"{CacheKeys.UserById}:{id}", user);

            return new OkObjectResult(user);
        }

        public async Task<IActionResult> ConfirmSkills(string id, UserCreateModel userCreateModel)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.Error("No userId was provided to ConfirmSkills");

                return new BadRequestObjectResult("Null or empty userId provided");
            }

            var validationResult = (await _userCreateModelValidator.ValidateAsync(userCreateModel)).PopulateModelState();
            if (validationResult != null)
            {
                return validationResult;
            }

            User user = await GetUserByUserIdInternal(id);

            if (user == null)
            {
                user = new User();
            }
            else
            {
                // Check if there is nothing to update, if so return without saving details
                if (user.HasConfirmedSkills && user.Username == userCreateModel.Username && user.Name == userCreateModel.Name)
                {
                    return new OkObjectResult(user);
                }
            }

            user.HasConfirmedSkills = true;
            user.UserId = id;
            user.Name = userCreateModel.Name;
            user.Username = userCreateModel.Username;
            user.HasConfirmedSkills = true;

            HttpStatusCode statusCode = await _userRepository.SaveUser(user);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to confirm skills in database with status code: {statusCode.ToString()}");

                return new InternalServerErrorResult("Failed to confirm skills");
            }

            await _cacheProvider.SetAsync($"{CacheKeys.UserById}:{id}", user);


            return new OkObjectResult(user);
        }

        private async Task<User> GetUserByUserIdInternal(string userId)
        {
            string cacheKey = $"{CacheKeys.UserById}:{userId}";

            User user = await _cacheProvider.GetAsync<User>(cacheKey);

            if (user == null)
            {
                user = await _userRepository.GetUserById(userId);

                if (user == null)
                {
                    _logger.Warning($"A user for id {userId} could not found");

                    return null;
                }
            }

            return user;
        }
    }
}
