﻿using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IPublishedProviderChannelVersionService
    {
        Task SavePublishedProviderVersionBody(
            string publishedProviderVersionId, 
            string publishedProviderVersionBody, 
            string specificationId,
            string channelCode);
    }
}
