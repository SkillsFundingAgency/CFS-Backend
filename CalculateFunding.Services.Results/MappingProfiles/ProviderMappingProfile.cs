using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Results.MappingProfiles
{
    public class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<Models.Results.ProviderSummary, CalculateFunding.Common.ApiClient.Providers.Models.ProviderSummary>();
        }
    }
}
