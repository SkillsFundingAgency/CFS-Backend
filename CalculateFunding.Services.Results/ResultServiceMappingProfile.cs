using AutoMapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Results
{
    public class ResultServiceMappingProfile : Profile
    {
        public ResultServiceMappingProfile()
        {
            CreateMap<Common.ApiClient.Profiling.Models.FinancialEnvelope, FinancialEnvelope>();
            CreateMap<Common.ApiClient.Profiling.Models.AllocationPeriodValue, AllocationPeriodValue>();
            CreateMap<Common.ApiClient.Profiling.Models.ProfilingPeriod, ProfilingPeriod>();

            CreateMap<AllocationLine, PublishedAllocationLineDefinition>();
            CreateMap<FundingRoute, PublishedFundingRoute>();

            CreateMap<FundingStream, PublishedFundingStreamDefinition>();
            CreateMap<PeriodType, PublishedPeriodType>();
        }
    }
}
