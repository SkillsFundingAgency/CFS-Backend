using AutoMapper;
using CalculateFunding.Models.Policy;

namespace CalculateFunding.Services.Results.MappingProfiles
{
    public class PolicyMappingProfile : Profile
    {
        public PolicyMappingProfile()
        {
            CreateMap<FundingStream, CalculateFunding.Common.ApiClient.Policies.Models.FundingStream>();
        }
    }
}
