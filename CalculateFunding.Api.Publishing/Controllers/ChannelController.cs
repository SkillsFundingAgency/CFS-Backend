using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class ChannelController : ControllerBase
    {
        private readonly IChannelsService _channelsService;

        public ChannelController(IChannelsService channelsService)
        {
            Guard.ArgumentNotNull(channelsService, nameof(channelsService));

            _channelsService = channelsService;
        }

        /// <summary>
        /// Retrieves all release management channels
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/releasemanagement/channels")]
        [Produces(typeof(IEnumerable<Channel>))]
        public async Task<IActionResult> GetAllChannels()
        {
            return await _channelsService.GetAllChannels();
        }

        /// <summary>
        /// Create or update an existing release channel
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("api/releasemanagement/channels")]
        [Produces(typeof(HttpStatusCode))]
        public async Task<IActionResult> UpsertChannel([FromBody] ChannelRequest request)
        {
            return await _channelsService.UpsertChannel(request);
        }
    }
}
