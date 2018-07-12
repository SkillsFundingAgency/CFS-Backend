namespace CalculateFunding.Models.External
{
    public class AllocationResult
    {
        public AllocationResult(AllocationLine allocationLine, int allocationVersionNumber, string allocationStatus,
            double aLlocationAmount, int allocationLearnerCount)
        {
            AllocationLine = allocationLine;
            AllocationVersionNumber = allocationVersionNumber;
            AllocationStatus = allocationStatus;
            ALlocationAmount = aLlocationAmount;
            AllocationLearnerCount = allocationLearnerCount;
        }

        public AllocationLine AllocationLine { get; set; }

        public int AllocationVersionNumber { get; set; }

        public string AllocationStatus { get; set; }

        public double ALlocationAmount { get; set; }

        public int AllocationLearnerCount { get; set; }
    }
}