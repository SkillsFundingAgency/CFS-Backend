namespace CalculateFunding.Models.Jobs
{
    public class JobLogUpdateModel
    {
        public int? ItemsProcessed { get; set; }

        public int? ItemsSucceeded { get; set; }

        public int? ItemsFailed { get; set; }
        
        public OutcomeType? OutcomeType { get; set; }

        /// <summary>
        /// Summary string of job log outcome
        /// eg Calculation engine ran for 1000 providers and completed successfully
        /// </summary>
        public string Outcome { get; set; }

        public bool? CompletedSuccessfully { get; set; }
    }
}
