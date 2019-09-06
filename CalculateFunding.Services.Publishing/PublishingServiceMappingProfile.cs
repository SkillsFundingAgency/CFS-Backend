using AutoMapper;
using CalculateFunding.Models.Publishing;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingServiceMappingProfile : Profile
    {
        public PublishingServiceMappingProfile()
        {
            CreateMap<ApiProvider, Provider>();
        }
    }
}
