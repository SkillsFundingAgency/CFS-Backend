﻿using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IChannelsService
    {
        Task<IActionResult> GetAllChannels();
        Task<IActionResult> UpsertChannel(ChannelRequest channelRequest);
        Task<IEnumerable<KeyValuePair<string, Channel>>> GetAndVerifyChannels(IEnumerable<string> channelCodes);
    }
}