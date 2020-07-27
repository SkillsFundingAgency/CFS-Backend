using System.Linq;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Specs.MappingProfiles
{
    public class SpecificationsMappingProfile : Profile
    {
        public SpecificationsMappingProfile()
        {
            CreateMap<Specification, SpecificationSummary>()
                .ForMember(_ => _.ProviderSource, opt => opt.MapFrom(s => s.Current.ProviderSource))
                .ForMember(_ => _.LastEditedDate, opt => opt.MapFrom(s => s.Current.Date))
                .ForMember(_ => _.Description, opt => opt.MapFrom(s => s.Current.Description))
                .ForMember(_ => _.FundingPeriod, opt => opt.MapFrom(s => s.Current.FundingPeriod))
                .ForMember(_ => _.FundingStreams, opt => opt.MapFrom(s => s.Current.FundingStreams))
                .ForMember(_ => _.ApprovalStatus, opt => opt.MapFrom(p => p.Current.PublishStatus))
                .ForMember(_ => _.ProviderVersionId, opt => opt.MapFrom(p => p.Current.ProviderVersionId))
                .ForMember(_ => _.TemplateIds, opt => opt.MapFrom(
                    p => p.Current.TemplateIds.ToDictionary(_ => _.Key, _ => _.Value)))
                .ForMember(m => m.DataDefinitionRelationshipIds, opt => opt.MapFrom(
                    p => p.Current.DataDefinitionRelationshipIds.ToArray()));

            CreateMap<SpecificationVersion, Models.Messages.SpecificationVersion>();

            CreateMap<Specification, SpecificationIndex>()
                .ForMember(_ => _.Description,
                    opt =>
                        opt.MapFrom(_ => _.Current.Description))
                .ForMember(_ => _.FundingStreamIds,
                    opt =>
                        opt.MapFrom(_ => (_.Current.FundingStreams ?? Enumerable.Empty<Reference>()).Select(fs => fs.Id).ToArray()))
                .ForMember(_ => _.FundingStreamNames,
                    opt =>
                        opt.MapFrom(_ => (_.Current.FundingStreams ?? Enumerable.Empty<Reference>()).Select(fs => fs.Name).ToArray()))
                .ForMember(_ => _.DataDefinitionRelationshipIds,
                    opt =>
                        opt.MapFrom(_ => _.Current.DataDefinitionRelationshipIds ?? new string[0]))
                .ForMember(_ => _.FundingPeriodId,
                    opt =>
                        opt.MapFrom(_ => _.Current.FundingPeriod.Id))
                .ForMember(_ => _.FundingPeriodName,
                    opt =>
                        opt.MapFrom(_ => _.Current.FundingPeriod.Name))
                .ForMember(_ => _.Status,
                    opt =>
                        opt.MapFrom(_ => _.Current.PublishStatus.ToString()))
                .ForMember(_ => _.LastUpdatedDate,
                    opt =>
                        opt.MapFrom(_ => _.Current.Date))
                .ForMember(_ => _.TotalMappedDataSets,
                    opt =>
                        opt.Ignore())
                .ForMember(_ => _.MapDatasetLastUpdated,
                    opt =>
                        opt.Ignore());
            
            CreateMap<SpecificationSearchModel, SpecificationIndex>()
                .ForMember(_ => _.FundingStreamIds,
                    opt =>
                        opt.MapFrom(_ => (_.FundingStreams ?? Enumerable.Empty<Reference>()).Select(fs => fs.Id).ToArray()))
                .ForMember(_ => _.FundingStreamNames,
                    opt =>
                        opt.MapFrom(_ => (_.FundingStreams ?? Enumerable.Empty<Reference>()).Select(fs => fs.Name).ToArray()))
                .ForMember(_ => _.DataDefinitionRelationshipIds,
                    opt =>
                        opt.MapFrom(_ => _.DataDefinitionRelationshipIds ?? new string[0]))
                .ForMember(_ => _.FundingPeriodId,
                    opt =>
                        opt.MapFrom(_ => _.FundingPeriod.Id))
                .ForMember(_ => _.FundingPeriodName,
                    opt =>
                        opt.MapFrom(_ => _.FundingPeriod.Name))
                .ForMember(_ => _.Status,
                    opt =>
                        opt.MapFrom(_ => _.PublishStatus))
                .ForMember(_ => _.LastUpdatedDate, 
                    opt => 
                        opt.MapFrom(_ => _.UpdatedAt)) .ForMember(_ => _.TotalMappedDataSets,
                    opt =>
                        opt.Ignore())
                .ForMember(_ => _.MapDatasetLastUpdated,
                    opt =>
                        opt.Ignore());

        }
    }
}
