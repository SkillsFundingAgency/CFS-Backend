using AutoMapper;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Datasets.MappingProfiles
{
    public class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<ProviderSummary, Common.ApiClient.Providers.Models.ProviderSummary>();
            CreateMap<Common.ApiClient.Providers.Models.Provider, ProviderSummary>()
                .ForMember(c => c.Id, opt => opt.Ignore());

            CreateMap<Common.ApiClient.Providers.Models.TrustStatus, TrustStatus>();
        }
    }
}
