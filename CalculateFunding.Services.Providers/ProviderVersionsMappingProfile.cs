using AutoMapper;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;

namespace CalculateFunding.Services.Providers
{
    public class ProviderVersionsMappingProfile : Profile
    {
        private const string MASTER_KEY = "master";

        public ProviderVersionsMappingProfile()
        {
            CreateMap<MasterProviderVersionViewModel, MasterProviderVersion>()
                .ForMember(c => c.ProviderVersionTypeString, opt => opt.Ignore())
                .ForMember(c => c.Name, opt => opt.Ignore())
                .ForMember(c => c.Description, opt => opt.Ignore())
                .ForMember(c => c.Version, opt => opt.Ignore())
                .ForMember(c => c.TargetDate, opt => opt.Ignore())
                .ForMember(c => c.FundingStream, opt => opt.Ignore())
                .ForMember(c => c.VersionType, opt => opt.Ignore())
                .ForMember(c => c.Created, opt => opt.Ignore())
                .ForMember(c => c.ProviderVersionId, opt => opt.MapFrom(c => c.ProviderVersionId))
                .ForMember(c => c.Id, opt => opt.MapFrom(s => MASTER_KEY));

            CreateMap<ProviderVersionViewModel, ProviderVersion>()
                .ForMember(c => c.ProviderVersionTypeString, opt => opt.Ignore())
                .ForMember(c => c.ProviderVersionId, opt => opt.MapFrom(c => c.ProviderVersionId))
                .ForMember(c => c.Providers, opt => opt.MapFrom(c => c.Providers))
                .ForMember(c => c.Id, opt => opt.MapFrom(s => MASTER_KEY));

            CreateMap<ProviderVersionMetadata, ProviderVersionMetadataDto>();

            CreateMap<Common.ApiClient.FundingDataZone.Models.Provider, Models.Providers.Provider>()
                .ForMember(c => c.ProviderProfileIdType, opt => opt.Ignore())
                .ForMember(c => c.Street, opt => opt.Ignore())
                .ForMember(c => c.Locality, opt => opt.Ignore())
                .ForMember(c => c.Address3, opt => opt.Ignore())
                .ForMember(c => c.PaymentOrganisationIdentifier, opt => opt.MapFrom(c => c.PaymentOrganisationUkprn))
                .ForMember(c => c.ProviderVersionIdProviderId, opt => opt.Ignore())
                .ForMember(c => c.ProviderVersionId, opt => opt.Ignore())
                .ForMember(c => c.TrustStatusViewModelString, opt => opt.Ignore());
        }
    }
}
