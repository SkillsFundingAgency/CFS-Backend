namespace CalculateFunding.Models.Jobs
{
    public class JobLogUpdateModel
    {
        public string JobId { get; set; }

        public int? ItemsProcessed { get; set; }

        public int? ItemsSucceeded { get; set; }

        public int? ItemsFailed { get; set; }

        public bool CompletedSuccessfully { get; set; }
    }
}
