namespace Allocations.Functions.Results.Models
{
    public abstract class ResultSummary
    {

        public decimal TotalAmount { get; set; }
        public TestSummary TestSummary { get; set; }    
    }
}