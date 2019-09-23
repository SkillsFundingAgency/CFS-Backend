using AutoMapper;
using CalculateFunding.Models.Publishing;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Publishing
{
    public class PublishingServiceMappingProfile : Profile
    {
        public PublishingServiceMappingProfile()
        {
            CreateMap<ApiProvider, Provider>();

            CreateMap<GeneratorModels.ReferenceData, TemplateModels.ReferenceData>()
                .ForMember(c => c.Format, opt => opt.Ignore())
                .ForMember(c => c.AggregationType, opt => opt.Ignore());
            CreateMap<GeneratorModels.Calculation, TemplateModels.Calculation>()
                .ForMember(c => c.AggregationType, opt => opt.Ignore())
                .ForMember(c => c.FormulaText, opt => opt.Ignore())
                .ForMember(c => c.ReferenceData, opt => opt.Ignore())
                .ForMember(c => c.ValueFormat, opt => opt.Ignore());
            CreateMap<GeneratorModels.FundingLine, TemplateModels.FundingLine>();

            CreateMap<GeneratorModels.FundingLine, FundingLine>();
            CreateMap<GeneratorModels.ReferenceData, FundingReferenceData>();
            CreateMap<GeneratorModels.Calculation, FundingCalculation>();

            CreateMap<TemplateModels.FundingLine, GeneratorModels.FundingLine>();
            CreateMap<TemplateModels.Calculation, GeneratorModels.Calculation>();
            CreateMap<TemplateModels.ReferenceData, GeneratorModels.ReferenceData>();

            CreateMap<TemplateModels.FundingLine, FundingLine>();
            CreateMap<TemplateModels.Calculation, FundingCalculation>();
            CreateMap<TemplateModels.ReferenceData, FundingReferenceData>();

            CreateMap<Generators.OrganisationGroup.Models.OrganisationIdentifier, PublishedOrganisationGroupTypeIdentifier>();
        }
    }
}
