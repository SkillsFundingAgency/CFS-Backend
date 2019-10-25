using AutoMapper;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Specs.MappingProfiles
{
    public class PolicyMappingProfile : Profile
    {
        public PolicyMappingProfile()
        {
            CreateMap<FundingStream, CalculateFunding.Common.ApiClient.Policies.Models.FundingStream>();
            CreateMap<CalculateFunding.Common.ApiClient.Policies.Models.FundingStream, FundingStream>();
        }
    }
}
