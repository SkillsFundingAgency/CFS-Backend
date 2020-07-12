using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedFundingWithProvider
    {
        public PublishedFunding PublishedFunding { get; set; }
        public IEnumerable<PublishedProvider> PublishedProviders { get; set; }
    }
}
