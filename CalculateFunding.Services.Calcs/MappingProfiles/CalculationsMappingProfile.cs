using System;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using Microsoft.AspNetCore.Routing.Constraints;
using GraphCalculation = CalculateFunding.Models.Graph.Calculation;
using GraphFundingLine = CalculateFunding.Models.Graph.FundingLine;

namespace CalculateFunding.Services.Calcs.MappingProfiles
{
    public class CalculationsMappingProfile : Profile
    {
        public CalculationsMappingProfile()
        {
            CreateMap<Common.ApiClient.Calcs.Models.Calculation, Calculation>();
            CreateMap<Common.ApiClient.Calcs.Models.BuildProject, BuildProject>();
            CreateMap<Common.ApiClient.Calcs.Models.Build, Build>();
            CreateMap<Common.ApiClient.Calcs.Models.SourceFile, SourceFile>();
            CreateMap<Common.ApiClient.Calcs.Models.CompilerMessage, CompilerMessage>();
            CreateMap<Common.ApiClient.Calcs.Models.Severity, Severity>();
            CreateMap<Common.ApiClient.Calcs.Models.SourceLocation, SourceLocation>();
            CreateMap<Common.ApiClient.Calcs.Models.CalculationSummary, CalculationSummaryModel>();
            CreateMap<Common.ApiClient.Calcs.Models.CalculationVersion, CalculationResponseModel>();
            CreateMap<DatasetRelationshipSummary, Common.ApiClient.Calcs.Models.DatasetRelationshipSummary>();
            CreateMap<DatasetDefinition, Common.ApiClient.Calcs.Models.Schema.DatasetDefinition>();
            CreateMap<TableDefinition, Common.ApiClient.Calcs.Models.Schema.TableDefinition>();
            CreateMap<FieldDefinition, Common.ApiClient.Calcs.Models.Schema.FieldDefinition>();
            CreateMap<FieldType, Common.ApiClient.Calcs.Models.Schema.FieldType>();
            CreateMap<IdentifierFieldType, Common.ApiClient.Calcs.Models.Schema.IdentifierFieldType>();
            CreateMap<Common.ApiClient.Calcs.Models.DatasetRelationshipSummary, DatasetRelationshipSummary>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.DatasetDefinition, DatasetDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.TableDefinition, TableDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.FieldDefinition, FieldDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.FieldType, FieldType>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.IdentifierFieldType, IdentifierFieldType>();

            CreateDatasetsMappingProfiles();
            CreateGraphMappingProfiles();
        }

        private void CreateDatasetsMappingProfiles()
        {
            CreateMap<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel, Models.Datasets.ViewModels.DatasetSpecificationRelationshipViewModel>();
            CreateMap<Common.ApiClient.DataSets.Models.DatasetDefinitionViewModel, Models.Datasets.ViewModels.DatasetDefinitionViewModel>();
            CreateMap<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel, Models.Datasets.DatasetSchemaRelationshipModel>();
            CreateMap<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipField, Models.Datasets.DatasetSchemaRelationshipField>();
            CreateMap<Common.ApiClient.DataSets.Models.DatasetDefinition, DatasetDefinition>();
            CreateMap<Common.ApiClient.DataSets.Models.TableDefinition, TableDefinition>();
            CreateMap<Common.ApiClient.DataSets.Models.FieldDefinition, FieldDefinition>();
            CreateMap<Common.ApiClient.DataSets.Models.FieldType, FieldType>();
            CreateMap<Common.ApiClient.DataSets.Models.IdentifierFieldType, IdentifierFieldType>();
        }

        private void CreateGraphMappingProfiles()
        {
            CreateMap<Common.ApiClient.Calcs.Models.Calculation, GraphCalculation>();
            CreateMap<Common.ApiClient.Graph.Models.FundingLine, GraphFundingLine>();
            CreateMap<Calculation, GraphCalculation>()
                .ForMember(dst => dst.CalculationId,
                    map => map.MapFrom(src => src.Current.CalculationId))
                .ForMember(dst => dst.CalculationName,
                    map => map.MapFrom(src => src.Current.Name))
                .ForMember(dst => dst.CalculationType,
                    map => map.MapFrom(src => src.Current.CalculationType))
                .ForMember(dst => dst.FundingStream,
                    map => map.MapFrom(src => src.FundingStreamId));

            CreateMap<Common.ApiClient.Specifications.Models.SpecificationSummary, Models.Graph.Specification>()
                .ForMember(dst => dst.SpecificationId, 
                    map => map.MapFrom(src => src.Id));

            CreateMap<FundingLine, GraphFundingLine>()
                .ForMember(dst => dst.FundingLineId,
                    map => map.MapFrom(src => $"{src.Namespace}_{src.Id}"))
                .ForMember(dst => dst.FundingLineName,
                    map => map.MapFrom(src => src.Name));

            CreateMap<GraphCalculation, Common.ApiClient.Graph.Models.Calculation>();
            CreateMap<GraphFundingLine, Common.ApiClient.Graph.Models.FundingLine>();
            CreateMap<Models.Graph.CalculationType, Common.ApiClient.Graph.Models.CalculationType>();
            CreateMap<Models.Graph.Specification, Common.ApiClient.Graph.Models.Specification>();
            CreateMap<Models.Graph.DataField, Common.ApiClient.Graph.Models.DataField>();
            CreateMap<Models.Graph.DatasetDefinition, Common.ApiClient.Graph.Models.DatasetDefinition>();
            CreateMap<Models.Graph.Dataset, Common.ApiClient.Graph.Models.Dataset>();
        }
    }
}
