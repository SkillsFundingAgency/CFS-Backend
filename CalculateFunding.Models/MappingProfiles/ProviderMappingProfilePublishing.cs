using AutoMapper;
using CalculateFunding.Models.Providers;
using System;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Models.MappingProfiles
{
    public class ProviderMappingProfilePublishing : Profile
    {
        public ProviderMappingProfilePublishing()
        {
            CreateMap<ApiProvider, Provider>();
        }
    }
}