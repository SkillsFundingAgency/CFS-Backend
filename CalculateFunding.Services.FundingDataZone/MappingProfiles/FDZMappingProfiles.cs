using AutoMapper;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone.MappingProfiles
{
    public class FDZMappingProfiles : Profile
    {
        public FDZMappingProfiles()
        {
            CreateMap<PublishingAreaProvider, Provider>();
        }
    }
}
