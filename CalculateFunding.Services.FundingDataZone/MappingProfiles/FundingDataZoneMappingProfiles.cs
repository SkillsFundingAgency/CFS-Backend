using AutoMapper;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone.MappingProfiles
{
    public class FundingDataZoneMappingProfiles : Profile
    {
        public FundingDataZoneMappingProfiles()
        {
            CreateMap<PublishingAreaProvider, Provider>();
        }
    }
}
 