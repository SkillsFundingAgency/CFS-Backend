using AutoMapper;
using CalculateFunding.Models.ProviderLegacy;

namespace CalculateFunding.Services.Datasets.MappingProfiles
{
    public class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<Common.ApiClient.Providers.Models.ProviderSummary, ProviderSummary>();
            CreateMap<ProviderSummary, Common.ApiClient.Providers.Models.ProviderSummary>();
            CreateMap<Common.ApiClient.Providers.Models.Provider, ProviderSummary>()
                .ForMember(c => c.Id, opt => opt.MapFrom(f => f.ProviderId));

            CreateMap<Common.ApiClient.Providers.Models.TrustStatus, TrustStatus>();
        }
    }
}
