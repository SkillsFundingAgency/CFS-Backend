﻿using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRefreshStateService
    {
        IDictionary<string, PublishedProviderVersion> ExistingCurrentPublishedProviders { get; set; }
        IDictionary<string, PublishedProvider> NewProviders { get; }
        IEnumerable<PublishedProvider> UpdatedProviders { get; }
        IEnumerable<PublishedProvider> AllProviders { get; }
        bool IsNewProvider(PublishedProvider publishedProvider);

        int Count { get; }

        void AddRange(IDictionary<string, PublishedProvider> publishedProviders);

        void Add(PublishedProvider publishedProvider);

        void Update(PublishedProvider publishedProvider);

        void Delete(PublishedProvider publishedProvider);

        bool Exclude(PublishedProvider publishedProvider);

        Task Persist(string jobId, Reference author, string correlationId);
    }
}
