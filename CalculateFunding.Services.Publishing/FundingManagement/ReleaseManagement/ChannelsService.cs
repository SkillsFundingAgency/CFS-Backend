using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
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
using System.Reflection;
using System.Threading.Tasks;
using DisplayAttribute = System.ComponentModel.DataAnnotations.DisplayAttribute;

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

        public async Task<IActionResult> PopulateReferenceData()
        {
            await PopulateGroupingReasons();
            await PopulateVariationReasons();

            return new OkResult();
        }

        public async Task<Dictionary<string, SqlModels.GroupingReason>> PopulateGroupingReasons()
        {
            IEnumerable<SqlModels.GroupingReason> existingGroupingReasons = await _repo.GetGroupingReasons();

            Dictionary<string, SqlModels.GroupingReason> groupingReasons = new Dictionary<string, SqlModels.GroupingReason>(
                existingGroupingReasons.ToDictionary(_ => _.GroupingReasonCode));

            IEnumerable<SqlModels.GroupingReason> expectedGroupingReasons = GenerateExpectedGroupingReasons();
            foreach (var reason in expectedGroupingReasons)
            {
                if (!groupingReasons.ContainsKey(reason.GroupingReasonCode))
                {
                    SqlModels.GroupingReason createdGroupingReason = await _repo.CreateGroupingReason(reason);
                    groupingReasons.Add(createdGroupingReason.GroupingReasonCode, createdGroupingReason);
                }
            }

            return groupingReasons;
        }

        public async Task<Dictionary<string, SqlModels.VariationReason>> PopulateVariationReasons()
        {
            IEnumerable<SqlModels.VariationReason> expectedVariationReasons = GenerateExpectedVariationReasons();

            IEnumerable<SqlModels.VariationReason> existingVariationReasons = await _repo.GetVariationReasons();

            Dictionary<string, SqlModels.VariationReason> variationReasons = new Dictionary<string, SqlModels.VariationReason>(existingVariationReasons.ToDictionary(_ => _.VariationReasonCode));

            foreach (var reason in expectedVariationReasons)
            {
                if (!variationReasons.ContainsKey(reason.VariationReasonCode))
                {
                    await _repo.CreateVariationReason(reason);
                    variationReasons.Add(reason.VariationReasonCode, reason);
                }
            }

            return variationReasons;
        }

        private IEnumerable<SqlModels.GroupingReason> GenerateExpectedGroupingReasons()
        {
            return new List<SqlModels.GroupingReason>()
            {
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 1,
                    GroupingReasonCode = nameof(GroupingReason.Payment),
                    GroupingReasonName = "Payment",
                },
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 2,
                    GroupingReasonCode = nameof(GroupingReason.Information),
                    GroupingReasonName = "Information",
                },
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 3,
                    GroupingReasonCode = nameof(GroupingReason.Contracting),
                    GroupingReasonName = "Contracting",
                },
                new SqlModels.GroupingReason()
                {
                    GroupingReasonId = 4,
                    GroupingReasonCode = nameof(GroupingReason.Indicative),
                    GroupingReasonName = "Indicative",
                }
            };
        }

        private IEnumerable<SqlModels.VariationReason> GenerateExpectedVariationReasons()
        {
            MemberInfo[] memberInfos = typeof(CalculateFunding.Models.Publishing.VariationReason).GetMembers(BindingFlags.Public | BindingFlags.Static);

            List<SqlModels.VariationReason> reasons = new List<SqlModels.VariationReason>();

            foreach (var member in memberInfos)
            {
                int? sqlConstantId = GetSqlId(member);
                if (sqlConstantId.HasValue)
                {
                    string description = GetDescription(member);

                    if (string.IsNullOrWhiteSpace(description))
                    {
                        description = member.Name;
                    }

                    reasons.Add(new SqlModels.VariationReason()
                    {
                        VariationReasonCode = member.Name,
                        VariationReasonId = sqlConstantId.Value,
                        VariationReasonName = description,
                    });
                }
            }

            return reasons;
        }

        private int? GetSqlId(MemberInfo member)
        {
            return member.GetCustomAttribute<SqlConstantIdAttribute>()?.Id;
        }

        private string GetDescription(MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttribute<DisplayAttribute>()?.Name;
        }
    }
}
