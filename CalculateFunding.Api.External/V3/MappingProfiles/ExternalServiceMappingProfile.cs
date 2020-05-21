using AutoMapper;

namespace CalculateFunding.Api.External.V3.MappingProfiles
{
    public class ExternalServiceMappingProfile : Profile
    {
        public ExternalServiceMappingProfile()
        {
            CreateMap<Common.ApiClient.Policies.Models.FundingStream, Models.FundingStream>();
        }
    }
}
