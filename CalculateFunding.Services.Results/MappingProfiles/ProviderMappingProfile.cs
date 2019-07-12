using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers.Models;
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
            CreateMap<CalculateFunding.Common.ApiClient.Providers.Models.Provider, Models.Results.ProviderSummary>()
                .ForMember(c => c.TrustStatus, opt => opt.MapFrom(c => Enum.Parse(typeof(TrustStatus), c.TrustStatusViewModelString)))
                .ForMember(c => c.Id, opt => opt.Ignore());
        }
    }
}
