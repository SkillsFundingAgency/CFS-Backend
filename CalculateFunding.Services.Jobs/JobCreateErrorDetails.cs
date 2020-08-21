using CalculateFunding.Models.Jobs;

namespace CalculateFunding.Services.Jobs
{
    public class JobCreateErrorDetails
    {
        public JobCreateModel CreateRequest { get; set; }
            
        public string Error { get; set; }
    }
}