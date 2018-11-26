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
        }
    }
}
