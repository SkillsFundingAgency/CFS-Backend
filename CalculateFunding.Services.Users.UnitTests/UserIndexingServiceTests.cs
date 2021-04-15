using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Users.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public class UserIndexingServiceTests
    {
        private readonly IUserIndexingService _userIndexingService;
        private readonly Mock<IUserRepository> _userRepository;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IJobManagement> _jobManagement;
        private readonly IMapper _mapper;
        private readonly Mock<ISearchRepository<UserIndex>> _userSearch;
        private readonly IUsersResiliencePolicies _usersResiliencePolicies;

        public UserIndexingServiceTests()
        {
            _userRepository = new Mock<IUserRepository>();
            _logger = new Mock<ILogger>();
            _jobManagement = new Mock<IJobManagement>();
            MapperConfiguration config = new MapperConfiguration(c => c.AddProfile<UsersMappingProfile>());
            _mapper = config.CreateMapper();
            _userSearch = new Mock<ISearchRepository<UserIndex>>();
            _usersResiliencePolicies = UsersResilienceTestHelper.GenerateTestPolicies();

            _userIndexingService = new UserIndexingService(
                _userRepository.Object,
                _logger.Object,
                _jobManagement.Object,
                _mapper,
                _userSearch.Object,
                _usersResiliencePolicies);
        }

        [TestMethod]
        public async Task ReIndex_ShouldQueueJobToReIndexUsers()
        {
            // Arrange
            Reference user = new Reference(NewRandomString(), NewRandomString());
            string correlationId = NewRandomString();
            Job reIndexUsersJob = new Job()
            {
                JobDefinitionId = JobConstants.DefinitionNames.ReIndexUsersJob,
                Id = NewRandomString()
            };

            _jobManagement.Setup(x => x.QueueJob(It.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.ReIndexUsersJob)))
                .ReturnsAsync(reIndexUsersJob);

            // Act
            IActionResult result = await _userIndexingService.ReIndex(user, correlationId);

            // Assert
            result.
                Should()
                .BeOfType<NoContentResult>();

            _jobManagement.Verify(x => x.QueueJob(It.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.ReIndexUsersJob)), Times.Once);
            _logger.Verify(x => x.Information(It.Is<string>(m => m == $"New job of type '{reIndexUsersJob.JobDefinitionId}' created with id: '{reIndexUsersJob.Id}'")), Times.Once);
        }

        [TestMethod]
        public async Task ReIndex_WhenQueueJobThrowsAnException_ThenItShouldReturnInternalServerErrorResult()
        {
            // Arrange
            Reference user = new Reference(NewRandomString(), NewRandomString());
            string correlationId = NewRandomString();
            string errorMessage = "Failed to queue users re-index job";
            Job reIndexUsersJob = new Job()
            {
                JobDefinitionId = JobConstants.DefinitionNames.ReIndexUsersJob,
                Id = NewRandomString()
            };

            _jobManagement.Setup(x => x.QueueJob(It.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.ReIndexUsersJob)))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            IActionResult result = await _userIndexingService.ReIndex(user, correlationId);

            // Assert
            result.
                Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(errorMessage);

            _jobManagement.Verify(x => x.QueueJob(It.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.ReIndexUsersJob)), Times.Once);
            _logger.Verify(x => x.Error(It.IsAny<Exception>(), It.Is<string>(m => m == errorMessage)), Times.Once);
        }

        [TestMethod]
        public async Task ProcessReIndexJob_WhenUsersAvailable_ThenStoreInUserIndexSearch()
        {
            // Arrange
            Message message = new Message();
            IEnumerable<User> users = new[] { new User() { UserId = NewRandomString() }, new User { UserId = NewRandomString() } };

            _userRepository.Setup(x => x.GetUsers()).ReturnsAsync(users);
            _userSearch.Setup(x => x.Index(It.Is<IEnumerable<UserIndex>>(u => u.All(i => users.Any(y => i.Id == y.UserId)))))
                .ReturnsAsync(Enumerable.Empty<IndexError>());

            // Act
            await _userIndexingService.Process(message);

            // Assert
            _userRepository.Verify(x => x.GetUsers(), Times.Once);
            _userSearch.Verify(x => x.Index(It.Is<IEnumerable<UserIndex>>(u => u.All(i => users.Any(y => i.Id == y.UserId)))), Times.Once);
        }

        [TestMethod]
        public void ProcessReIndexJob_WhenSearchUserIndexRetrunIndexErrors_ThenItShouldThrowAnException()
        {
            // Arrange
            Message message = new Message();
            IEnumerable<User> users = new[] { new User() { UserId = NewRandomString() }, new User { UserId = NewRandomString() } };
            IndexError indexError = new IndexError() { ErrorMessage = NewRandomString() };

            _userRepository.Setup(x => x.GetUsers()).ReturnsAsync(users);
            _userSearch.Setup(x => x.Index(It.Is<IEnumerable<UserIndex>>(u => u.All(i => users.Any(y => i.Id == y.UserId)))))
                .ReturnsAsync(new[] { indexError});

            // Act
            Func<Task> invocation = async() => await _userIndexingService.Process(message);

            // Assert
            invocation
                .Should()
                .Throw<FailedToIndexSearchException>()
                .Which
                .Message
                .Should()
                .Contain($"failed to index search with the following errors: {indexError.ErrorMessage}");

            _userRepository.Verify(x => x.GetUsers(), Times.Once);
        }

        private static string NewRandomString() => new RandomString();
    }
}
