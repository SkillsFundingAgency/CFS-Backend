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
            CreateMap<PublishingAreaOrganisation, PaymentOrganisation>()
                .ForMember(_ => _.Name, 
                    opt => opt.MapFrom(_ => _.LaCode))
                .ForMember(_ => _.OrganisationType, 
                    opt => opt.MapFrom(_ => _.PaymentOrganisationType));
            CreateMap<PublishingAreaProviderSnapshot, ProviderSnapshot>();
        }
    }
}
 