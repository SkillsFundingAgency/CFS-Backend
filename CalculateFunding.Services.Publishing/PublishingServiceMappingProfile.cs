using AutoMapper;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingServiceMappingProfile : Profile
    {
        public PublishingServiceMappingProfile()
        {
            CreateMap<Common.ApiClient.Providers.Models.Provider, Provider>()
                .ForMember(d => d.TrustStatus, opt => opt.MapFrom(
                    o => EnumExtensions.AsEnum<ProviderTrustStatus>(o.TrustStatusViewModelString)));
        }
    }
}
