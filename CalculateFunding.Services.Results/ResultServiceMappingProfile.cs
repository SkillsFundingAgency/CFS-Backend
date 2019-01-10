using AutoMapper;

namespace CalculateFunding.Services.Results
{
    public class ResultServiceMappingProfile : Profile
    {
        public ResultServiceMappingProfile()
        {
            CreateMap<Common.ApiClient.Profiling.Models.FinancialEnvelope, Models.Results.FinancialEnvelope>();
            CreateMap<Common.ApiClient.Profiling.Models.AllocationPeriodValue, Models.Results.AllocationPeriodValue>();
            CreateMap<Common.ApiClient.Profiling.Models.ProfilingPeriod, Models.Results.ProfilingPeriod>();
        }
    }
}
