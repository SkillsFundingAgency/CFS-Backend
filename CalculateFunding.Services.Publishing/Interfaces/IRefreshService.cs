﻿using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRefreshService
    {
        Task RefreshResults(Message message, int deliveryCount = 1);
    }
}
