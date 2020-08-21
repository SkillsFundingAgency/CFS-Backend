namespace CalculateFunding.Models.Jobs
{
    public class JobCreateResult
    {
        public JobCreateModel CreateRequest { get; set; }
        
        public Job Job { get; set; }
        
        public string Error { get; set; }

        public bool WasCreated => Job != null;
        
        public bool WasQueued { get; set; }

        public bool HasError => !WasCreated || !WasQueued;
    }
}