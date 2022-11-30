using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Models
{
    public class FundingChannelVersion
    {
        public string FundingId { get;set; }
        public string ChannelCode { get;set; }
        public int ChannelVersion { get;set; }
    }
}
