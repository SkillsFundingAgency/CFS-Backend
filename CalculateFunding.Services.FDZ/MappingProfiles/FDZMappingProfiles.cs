using AutoMapper;
using CalculateFunding.Models.FDZ;
using CalculateFunding.Services.FDZ.SqlModels;

namespace CalculateFunding.Services.FDZ.MappingProfiles
{
    public class FDZMappingProfiles : Profile
    {
        public FDZMappingProfiles()
        {
            CreateMap<PublishingAreaProvider, Provider>();
        }
    }
}
