using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Users;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Users.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
namespace CalculateFunding.Services.Users
{
    public class UserIndexingService : JobProcessingService, IUserIndexingService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger _logger;
        private readonly IJobManagement _jobManagement;
        private readonly IMapper _mapper;
        private readonly ISearchRepository<UserIndex> _userSearch;
        private readonly AsyncPolicy _usersSearchPolicy;

        public UserIndexingService(
            IUserRepository userRepository,
            ILogger logger,
            IJobManagement jobManagement,
            IMapper mapper,
            ISearchRepository<UserIndex> userSearch,
            IUsersResiliencePolicies usersResiliencePolicies) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(userRepository, nameof(userRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(userSearch, nameof(userSearch));
            Guard.ArgumentNotNull(usersResiliencePolicies, nameof(usersResiliencePolicies));
            Guard.ArgumentNotNull(usersResiliencePolicies.UsersSearchRepository, nameof(usersResiliencePolicies.UsersSearchRepository));

            _userRepository = userRepository;
            _logger = logger;
            _jobManagement = jobManagement;
            _mapper = mapper;
            _userSearch = userSearch;
            _usersSearchPolicy = usersResiliencePolicies.UsersSearchRepository;
        }

        public async Task<IActionResult> ReIndex(Reference user, string correlationId)
        {
            Guard.ArgumentNotNull(user, nameof(user));
            Guard.IsNullOrWhiteSpace(correlationId, nameof(correlationId));

            try
            {
                await CreateReIndexJob(user, correlationId);
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
            

            return new NoContentResult();
        }

        public async Task<Job> CreateReIndexJob(Reference user, string correlationId)
        {
            try
            {
                Job job = await _jobManagement.QueueJob(new JobCreateModel
                {
                    JobDefinitionId = JobConstants.DefinitionNames.ReIndexUsersJob,
                    InvokerUserId = user?.Id,
                    CorrelationId = correlationId,
                    Trigger = new Trigger
                    {
                        Message = "ReIndexing Users",
                        EntityType = nameof(User)
                    }
                });

                if (job != null)
                {
                    _logger.Information($"New job of type '{job.JobDefinitionId}' created with id: '{job.Id}'");
                }
                else
                {
                    string errorMessage = $"Failed to create job of type '{JobConstants.DefinitionNames.ReIndexUsersJob}'";
                    _logger.Error(errorMessage);
                }

                return job;
            }
            catch (Exception ex)
            {
                string error = "Failed to queue users re-index job";

                _logger.Error(ex, error);
                throw new Exception(error);
            }
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            IEnumerable<User> users = await _userRepository.GetUsers();
            IEnumerable<UserIndex> userIndexes = _mapper.Map<IEnumerable<UserIndex>>(users);

            IEnumerable<IndexError> indexingErrors = await _usersSearchPolicy.ExecuteAsync(() => _userSearch.Index(userIndexes));

            if (!indexingErrors.IsNullOrEmpty())
            {
                string indexingErrorMessages = indexingErrors.Select(_ => _.ErrorMessage).Join(". ");
                string userIds = users.Select(_ => _.Id).Join(", ");

                _logger.Error($"Could not index Users {userIds} because: {indexingErrorMessages}");

                throw new FailedToIndexSearchException(indexingErrors);
            }
        }
    }
}
