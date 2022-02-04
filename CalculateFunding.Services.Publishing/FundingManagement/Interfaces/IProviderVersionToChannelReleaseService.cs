using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IProviderVersionToChannelReleaseService
    {
        Task ReleaseProviderVersionChannel(IEnumerable<string> releasedProviderIds, int channelId, DateTime statusChangedDateTime);
    }
}
