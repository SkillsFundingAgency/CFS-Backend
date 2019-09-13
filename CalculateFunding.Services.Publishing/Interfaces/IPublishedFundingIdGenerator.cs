using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingIdGenerator
    {
        string GetFundingId(PublishedFundingVersion publishedFundingVersion);
    }
}
