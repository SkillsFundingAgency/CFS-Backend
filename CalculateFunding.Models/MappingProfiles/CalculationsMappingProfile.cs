using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Models.MappingProfiles
{
    public class CalculationsMappingProfile : Profile
    {
        public CalculationsMappingProfile()
        {
            CreateMap<Common.ApiClient.Calcs.Models.Calculation, Calculation>();
            CreateMap<Common.ApiClient.Calcs.Models.BuildProject, BuildProject>();
            CreateMap<Common.ApiClient.Calcs.Models.CalculationSummaryModel, CalculationSummaryModel>();
            CreateMap<Common.ApiClient.Calcs.Models.CalculationCurrentVersion, CalculationCurrentVersion>();
            CreateMap<DatasetRelationshipSummary, Common.ApiClient.Calcs.Models.DatasetRelationshipSummary>();
            CreateMap<DatasetDefinition, Common.ApiClient.Calcs.Models.Schema.DatasetDefinition>();
        }
    }
}
