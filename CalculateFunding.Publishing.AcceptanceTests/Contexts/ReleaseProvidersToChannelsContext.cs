using CalculateFunding.Models.Publishing.FundingManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class ReleaseProvidersToChannelsContext : IReleaseProvidersToChannelsContext
    {
        public ReleaseProvidersToChannelRequest Request { get; set; } = new ReleaseProvidersToChannelRequest();
    }
}
