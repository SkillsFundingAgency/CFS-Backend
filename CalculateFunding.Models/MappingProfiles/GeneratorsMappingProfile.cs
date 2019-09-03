using AutoMapper;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Models.MappingProfiles
{
    public class GeneratorsMappingProfile : Profile
    {
        public GeneratorsMappingProfile()
        {
            CreateMap<GeneratorModels.Calculation, TemplateModels.Calculation>()
                .ForMember(c => c.AggregationType, opt => opt.Ignore())
                .ForMember(c => c.FormulaText, opt => opt.Ignore())
                .ForMember(c => c.ReferenceData, opt => opt.Ignore())
                .ForMember(c => c.ValueFormat, opt => opt.Ignore());


            CreateMap<GeneratorModels.FundingLine, TemplateModels.FundingLine>();

            CreateMap<Publishing.FundingLine, GeneratorModels.FundingLine>()
                .ForMember(c => c.Calculations, opt => opt.Ignore())
                .ForMember(c => c.FundingLines, opt => opt.Ignore());

            CreateMap<GeneratorModels.Calculation, Publishing.FundingCalculation>()
                .ForMember(c => c.AggregationType, opt => opt.Ignore());
        }
    }
}
