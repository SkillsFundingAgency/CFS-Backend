namespace Allocations.Functions.Engine.Models
{
    public abstract class ResultSummary
    {

        public decimal TotalAmount { get; set; }
        public TestSummary TestSummary { get; set; }    
    }
}