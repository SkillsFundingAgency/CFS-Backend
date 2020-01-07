using AutoMapper;
using CalculateFunding.Models.Jobs;

namespace CalculateFunding.Models.MappingProfiles
{
    public class JobsMappingProfile : Profile
    {
        public JobsMappingProfile()
        {
            CreateMap<Job, JobViewModel>()
                .ForMember(m => m.ChildJobs, opt => opt.Ignore());

            CreateMap<Job, JobSummary>()
                .ForMember(m => m.EntityId, opt => opt.MapFrom(j => j.Trigger.EntityId))
                .ForMember(m => m.JobId, opt => opt.MapFrom(j => j.Id))
                .ForMember(m => m.JobType, opt => opt.MapFrom(j => j.JobDefinitionId));
        }
    }
}
