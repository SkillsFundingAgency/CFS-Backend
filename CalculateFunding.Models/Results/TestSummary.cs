namespace CalculateFunding.Models.Results
{
    public class TestSummary
    {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Covered => Passed + Failed;
        public decimal Coverage { get; set; }
        public decimal PassedRate { get; set; }
        public decimal FailedRate { get; set; }
    }
}