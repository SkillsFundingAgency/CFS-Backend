using AutoMapper;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Calcs.MappingProfiles
{
    public class PolicyMappingProfile : Profile
    {
        public PolicyMappingProfile()
        {
            CreateMap<FundingStream, CalculateFunding.Common.ApiClient.Policies.Models.FundingStream>();
        }
    }
}
