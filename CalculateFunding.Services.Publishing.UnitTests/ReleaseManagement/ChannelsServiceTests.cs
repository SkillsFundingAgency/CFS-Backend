using AutoMapper;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Builders;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ChannelsServiceTests
    {
        private Mock<IReleaseManagementRepository> _releaseManagmentRepo;
        private Mock<IValidator<ChannelRequest>> _validator;
        private Mock<ILogger> _logger;

        private ChannelsService _service;

        [TestInitialize]
        public void Initialise()
        {
            _releaseManagmentRepo = new Mock<IReleaseManagementRepository>();
            _validator = new Mock<IValidator<ChannelRequest>>();
            _logger = new Mock<ILogger>();
            IMapper mapper = CreateMapper();

            _service = new ChannelsService(_releaseManagmentRepo.Object, _validator.Object, mapper, _logger.Object);
        }

        [TestMethod]
        public async Task GetRetrievesAllChannels()
        {
            List<Channel> expectedChannels = new List<Channel>()
            {
                new Channel
                {
                    ChannelId = 1,
                    ChannelCode = "Statements",
                    ChannelName = "Statements",
                    UrlKey = "statements"
                },
                new Channel
                {
                    ChannelId = 2,
                    ChannelCode = "Payments",
                    ChannelName = "Payments",
                    UrlKey = "payments"
                },
            };

            GivenTheDatabaseState(expectedChannels);
            IActionResult result = await WhenAllChannelsAreRetrieved();

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo(expectedChannels);
        }

        [TestMethod]
        [DataRow(new string[] { "Statements", "Contracts"})]
        [DataRow(new string[] { "Statements", "Contracts", "Payments" })]
        public async Task GetAndVerifyChannels_RetrievesSpecifiedChannels(string[] channelCodes)
        {
            Dictionary<string, Channel> channels = new Dictionary<string, Channel>()
            {
                {
                    "Statements",
                    new Channel
                    {
                        ChannelId = 1,
                        ChannelCode = "Statements",
                        ChannelName = "Statements",
                        UrlKey = "statements"
                    }
                },
                {
                    "Payments",
                    new Channel
                    {
                        ChannelId = 2,
                        ChannelCode = "Payments",
                        ChannelName = "Payments",
                        UrlKey = "payments"
                    }
                },
                {
                    "Contracts",
                    new Channel
                    {
                        ChannelId = 3,
                        ChannelCode = "Contracts",
                        ChannelName = "Contracts",
                        UrlKey = "contracts"
                    }
                }
            };

            GivenTheDatabaseState(channels.Select(s => s.Value));
            List<string> requiredChannels = channelCodes.ToList();

            IEnumerable<KeyValuePair<string, Channel>> result = await WhenGetAndVerifyChannels(requiredChannels);

            result
                .Should()
                .BeEquivalentTo(channels.Where(c => requiredChannels.Contains(c.Key)));
        }

        [TestMethod]
        [DataRow(new string[] { "" })]
        [DataRow(new string[] { "Invoices" })]
        [DataRow(new string[] { "Invoices", "Contracts" })]
        public async Task GetAndVerifyChannels_Throws_IfChannelCodeNotExist(string[] channelCodes)
        {
            Dictionary<string, Channel> channels = new Dictionary<string, Channel>()
            {
                {
                    "Statements",
                    new Channel
                    {
                        ChannelId = 1,
                        ChannelCode = "Statements",
                        ChannelName = "Statements",
                        UrlKey = "statements"
                    }
                },
                {
                    "Payments",
                    new Channel
                    {
                        ChannelId = 2,
                        ChannelCode = "Payments",
                        ChannelName = "Payments",
                        UrlKey = "payments"
                    }
                },
                {
                    "Contracts",
                    new Channel
                    {
                        ChannelId = 3,
                        ChannelCode = "Contracts",
                        ChannelName = "Contracts",
                        UrlKey = "contracts"
                    }
                }
            };

            GivenTheDatabaseState(channels.Select(s => s.Value));
            List<string> requiredChannels = channelCodes.ToList();

            Func<Task> result = async () => await WhenGetAndVerifyChannels(requiredChannels);

            result
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .And.Message
                .Should()
                .Contain("does not exist");

        }

        [TestMethod]
        public async Task UpsertReturnsBadRequestIfChannelRequestIsNull()
        {
            IActionResult result = await WhenChannelIsUpserted(null);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            ((BadRequestObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo("Empty model provided for channel request");
        }

        [TestMethod]
        public async Task UpsertReturnsBadRequestIfValidationFailure()
        {
            ChannelRequest request = new ChannelRequest
            {
                ChannelCode = "Statement",
                UrlKey = "statements"
            };

            GivenTheValidationResult(request, NewValidationFailure());

            IActionResult result = await WhenChannelIsUpserted(request);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SerializableError>()
                .Which
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task UpsertCreatesNewChannelIfOneDoesNotExist()
        {
            Channel expectedChannel = new Channel
            {
                ChannelCode = "Statement",
                ChannelName = "Statement",
                UrlKey = "statements"
            };

            ChannelRequest request = new ChannelRequest
            {
                ChannelCode = "Statement",
                ChannelName = "Statement",
                UrlKey = "statements"
            };

            GivenTheValidationResult(request, NewValidationResult());
            GivenCreateChannelSuccessful(expectedChannel);

            IActionResult result = await WhenChannelIsUpserted(request);

            ThenNewChannelWasCreated(expectedChannel);
            ThenExistingChannelWasNotUpdated();

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo(expectedChannel);
        }

        [TestMethod]
        public async Task UpsertUpdatesChannelIfOneDoesExist()
        {
            Channel existingChannel = new Channel
            {
                ChannelCode = "Statement",
                ChannelName = "Statement",
                UrlKey = "statements"
            };

            Channel expectedChannel = new Channel
            {
                ChannelCode = "Statement",
                ChannelName = "Statement - Updated",
                UrlKey = "statements"
            };

            ChannelRequest request = new ChannelRequest
            {
                ChannelCode = "Statement",
                ChannelName = "Statement - Updated",
                UrlKey = "statements"
            };

            GivenTheValidationResult(request, NewValidationResult());
            GivenTheExistingChannel(existingChannel, existingChannel.ChannelCode);
            GivenUpdateChannelSuccessful();

            IActionResult result = await WhenChannelIsUpserted(request);

            ThenExistingChannelWasUpdated(expectedChannel);
            ThenNewChannelWasNotCreated();

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo(expectedChannel);
        }

        [TestMethod]
        public async Task UpdateReturnsBadRequestIfUpdateFails()
        {
            Channel existingChannel = new Channel
            {
                ChannelCode = "Statement",
                ChannelName = "Statement",
                UrlKey = "statements"
            };

            ChannelRequest request = new ChannelRequest
            {
                ChannelCode = "Statement",
                ChannelName = "Statement - Updated",
                UrlKey = "statements"
            };

            GivenTheValidationResult(request, NewValidationResult());
            GivenTheExistingChannel(existingChannel, existingChannel.ChannelCode);
            GivenUpdateChannelUnsuccessful();

            IActionResult result = await WhenChannelIsUpserted(request);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            ((BadRequestObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo("Channel could not be updated");
        }

        [TestMethod]
        public void UpsertThrowsInvalidOperationExceptionIfSaveFails()
        {
            Channel existingChannel = new Channel
            {
                ChannelId = 1,
                ChannelCode = "Statements",
                ChannelName = "Statements",
                UrlKey = "statements"
            };

            ChannelRequest request = new ChannelRequest
            {
                ChannelCode = "Statements",
                ChannelName = "Statements",
                UrlKey = "statements"
            };

            GivenTheExistingChannel(existingChannel, existingChannel.ChannelCode);
            GivenTheValidationResult(request, NewValidationResult());
            AndUpdateChannelThrowsAnException();

            Func<Task<IActionResult>> invocation = () => WhenChannelIsUpserted(request);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Unable to upsert release channel {request.ChannelCode}");

            _logger
                .Verify(_ => _.Error(It.IsAny<Exception>(), $"Unable to upsert release channel {request.ChannelCode}"), Times.Once);
        }

        private void GivenCreateChannelSuccessful(Channel channel)
        {
            _releaseManagmentRepo.Setup(_ => _.CreateChannel(It.IsAny<Channel>()))
                .ReturnsAsync(channel);
        }

        private void GivenUpdateChannelSuccessful()
        {
            _releaseManagmentRepo.Setup(_ => _.UpdateChannel(It.IsAny<Channel>()))
                .ReturnsAsync(true);
        }

        private void GivenUpdateChannelUnsuccessful()
        {
            _releaseManagmentRepo.Setup(_ => _.UpdateChannel(It.IsAny<Channel>()))
                .ReturnsAsync(false);
        }

        private void ThenNewChannelWasCreated(Channel channel)
        {
            _releaseManagmentRepo
                .Verify(_ => _.CreateChannel(It.Is<Channel>(c => c.ChannelCode == channel.ChannelCode)),
                    Times.Once);
        }

        private void ThenNewChannelWasNotCreated()
        {
            _releaseManagmentRepo
                .Verify(_ => _.CreateChannel(It.IsAny<Channel>()),
                    Times.Never);
        }

        private void ThenExistingChannelWasUpdated(Channel channel)
        {
            _releaseManagmentRepo
                .Verify(_ => _.UpdateChannel(It.Is<Channel>(c => c.ChannelCode == channel.ChannelCode)),
                    Times.Once);
        }

        private void ThenExistingChannelWasNotUpdated()
        {
            _releaseManagmentRepo
                .Verify(_ => _.UpdateChannel(It.IsAny<Channel>()),
                    Times.Never);
        }

        public void GivenTheDatabaseState(IEnumerable<Channel> channels)
        {
            _releaseManagmentRepo.Setup(_ => _.GetChannels())
                .ReturnsAsync(channels.ToList());
        }

        public void GivenTheExistingChannel(Channel channel, string channelCode)
        {
            _releaseManagmentRepo.Setup(_ => _.GetChannelByChannelCode(It.Is<string>(c => c == channelCode)))
                .ReturnsAsync(channel);
        }

        public void AndUpdateChannelThrowsAnException()
        {
            _releaseManagmentRepo.Setup(_ => _.UpdateChannel(It.IsAny<Channel>()))
                .ThrowsAsync(new Exception());
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder builder = new ValidationResultBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ValidationResult NewValidationFailure(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder builder = new ValidationResultBuilder();
            builder.WithValidationFailures(new ValidationFailure("ChannelName", "Must not be null"));

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private void GivenTheValidationResult(ChannelRequest request, ValidationResult validationResult)
        {
            _validator.Setup(_ => _.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
        }

        private async Task<IActionResult> WhenChannelIsUpserted(ChannelRequest request)
        {
            return await _service.UpsertChannel(request);
        }

        private async Task<IActionResult> WhenAllChannelsAreRetrieved()
        {
            return await _service.GetAllChannels();
        }

        private async Task<IEnumerable<KeyValuePair<string, Channel>>> WhenGetAndVerifyChannels(IEnumerable<string> channelCodes)
        {
            return await _service.GetAndVerifyChannels(channelCodes);
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            });

            return new Mapper(config);
        }
    }
}
