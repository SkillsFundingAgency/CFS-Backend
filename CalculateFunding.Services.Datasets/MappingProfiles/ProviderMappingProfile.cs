using AutoMapper;
using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Datasets.MappingProfiles
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
