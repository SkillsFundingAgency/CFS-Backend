using AutoMapper;
using CalculateFunding.Models.Obsoleted;

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
