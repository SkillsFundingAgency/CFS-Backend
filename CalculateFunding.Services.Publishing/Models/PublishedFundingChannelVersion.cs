using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedFundingChannelVersion:FundingChannelVersion
    {
        public int ProviderCount { get; set; }
        public string ProviderName { get; set; }
        public string ProviderUKPRN { get; set; }
        public string ProviderURN { get; set; }
        public string ProviderUPIN { get; set; }
        public string ProviderLACode { get; set; }
        public string ProviderStatus { get; set; }
        public string ProviderSuccessor { get; set; }
        public string ProviderPredecessors { get; set; }
        public string ProviderVariationReasons { get; set; }

    }
}
