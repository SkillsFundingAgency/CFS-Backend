using AutoMapper;
using PolicyApiClientModel = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Api.External.V3.MappingProfiles
{
    public class ExternalServiceMappingProfile : Profile
    {
        public ExternalServiceMappingProfile()
        {
            CreateMap<PolicyApiClientModel.FundingStream, Models.FundingStream>();
            CreateMap<PolicyApiClientModel.FundingPeriod, Models.FundingPeriod>();
        }
    }
}
