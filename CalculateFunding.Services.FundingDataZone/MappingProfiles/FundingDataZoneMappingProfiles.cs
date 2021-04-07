using AutoMapper;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.SqlModels;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.FundingDataZone.MappingProfiles
{
    public class FundingDataZoneMappingProfiles : Profile
    {
        public FundingDataZoneMappingProfiles()
        {
            CreateMap<PublishingAreaProvider, Provider>()
                .ForMember(_ => _.Predecessors, opt => opt.MapFrom<PredecessorsResolver>())
                .ForMember(_ => _.Successors, opt => opt.MapFrom<SuccessorsResolver>())
                .ForMember(_ => _.PaymentOrganisationType, opt => opt.Ignore());
            CreateMap<PublishingAreaOrganisation, PaymentOrganisation>()
                .ForMember(_ => _.Name, 
                    opt => opt.MapFrom(_ => _.LaCode))
                .ForMember(_ => _.OrganisationType, 
                    opt => opt.MapFrom(_ => _.PaymentOrganisationType));
            CreateMap<PublishingAreaProviderSnapshot, ProviderSnapshot>();
        }
    }

    public class PredecessorsResolver : IValueResolver<PublishingAreaProvider, Provider, IEnumerable<string>>
    {
        public IEnumerable<string> Resolve(PublishingAreaProvider source, Provider destination, IEnumerable<string> member, ResolutionContext context)
        {
            return source.Predecessors?.Split(",").ToList();
        }
    }

    public class SuccessorsResolver : IValueResolver<PublishingAreaProvider, Provider, IEnumerable<string>>
    {
        public IEnumerable<string> Resolve(PublishingAreaProvider source, Provider destination, IEnumerable<string> member, ResolutionContext context)
        {
            return source.Successors?.Split(",").ToList();
        }
    }
}
 