﻿using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Services.Results.MappingProfiles
{
    public class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<Models.Results.ProviderSummary, ProviderSummary>();
            CreateMap<Provider, Models.Results.ProviderSummary>()
                .ForMember(c => c.Id, opt => opt.Ignore());

            CreateMap<TrustStatus, TrustStatus>();
        }
    }
}
