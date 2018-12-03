namespace CalculateFunding.Models.Jobs
{
    public class Trigger
    {
        /// <summary>
        /// Required: Human readable message describing the trigger
        /// eg Specification data map change
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Trigger entity ID, eg Calculation Specification ID
        /// Optional depending on JobType configuration.
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Trigger Entity Type
        /// eg CalculationSpecification
        /// Optional depending on JobType configuration.
        /// </summary>
        public string EntityType { get; set; }
    }
}
