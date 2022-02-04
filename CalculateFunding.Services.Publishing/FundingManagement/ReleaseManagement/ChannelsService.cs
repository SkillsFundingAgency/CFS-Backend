using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ChannelsService : IChannelsService
    {
        private readonly ILogger _logger;
        private readonly IReleaseManagementRepository _repo;
        private readonly IMapper _mapper;
        private readonly IValidator<ChannelRequest> _channelValidator;

        public ChannelsService(IReleaseManagementRepository releaseManagementRepository,
                               IValidator<ChannelRequest> channelValidator,
                               IMapper mapper,
                               ILogger logger)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(channelValidator, nameof(channelValidator));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _repo = releaseManagementRepository;
            _channelValidator = channelValidator;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> GetAllChannels()
        {
            IEnumerable<Channel> channels = await _repo.GetChannels();
            return new OkObjectResult(channels);
        }

        public async Task<IActionResult> UpsertChannel(ChannelRequest channelRequest)
        {
            if (channelRequest == null)
            {
                return new BadRequestObjectResult("Empty model provided for channel request");
            }

            ValidationResult validationResult = await _channelValidator.ValidateAsync(channelRequest);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            try
            {
                Channel channel = null;
                Channel channelToBeCreatedOrUpdated = _mapper.Map<Channel>(channelRequest);
                Channel existingChannel = await _repo.GetChannelByChannelCode(channelToBeCreatedOrUpdated.ChannelCode);

                if (existingChannel == null)
                {
                    channel = await _repo.CreateChannel(channelToBeCreatedOrUpdated);
                    return new OkObjectResult(channel);
                }
                else
                {
                    channelToBeCreatedOrUpdated.ChannelId = existingChannel.ChannelId;
                    bool updateResult = await _repo.UpdateChannel(channelToBeCreatedOrUpdated);
                    if (!updateResult)
                    {
                        return new BadRequestObjectResult("Channel could not be updated");
                    }
                    return new OkObjectResult(channelToBeCreatedOrUpdated);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Unable to upsert release channel {channelRequest.ChannelCode}");
                throw new InvalidOperationException($"Unable to upsert release channel {channelRequest.ChannelCode}", e);
            }
        }

        public async Task<IEnumerable<KeyValuePair<string, Channel>>> GetAndVerifyChannels(IEnumerable<string> channelCodes)
        {
            Dictionary<string, Channel> allChannels = (await _repo.GetChannels()).ToDictionary(_ => _.ChannelCode);

            Dictionary<string, Channel> channels = new Dictionary<string, Channel>(channelCodes.Count());

            foreach (string channelCode in channelCodes)
            {
                if (!allChannels.ContainsKey(channelCode))
                {
                    throw new InvalidOperationException($"Channel with code '{channelCode}' does not exist");
                }

                channels.Add(channelCode, allChannels[channelCode]);
            }

            return channels;
        }
    }
}
