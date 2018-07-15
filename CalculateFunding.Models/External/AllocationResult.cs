using System;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class AllocationResult
    {
        public AllocationResult()
        {
        }

        public AllocationResult(AllocationLine allocationLine, ushort allocationVersionNumber, string allocationStatus,
            decimal aLlocationAmount, uint? allocationLearnerCount)
        {
            AllocationLine = allocationLine;
            AllocationVersionNumber = allocationVersionNumber;
            AllocationStatus = allocationStatus;
            AllocationAmount = aLlocationAmount;
            AllocationLearnerCount = allocationLearnerCount;
        }

        public AllocationLine AllocationLine { get; set; }

        public ushort AllocationVersionNumber { get; set; }

        public string AllocationStatus { get; set; }

        public decimal AllocationAmount { get; set; }

        public uint? AllocationLearnerCount { get; set; }
    }
}