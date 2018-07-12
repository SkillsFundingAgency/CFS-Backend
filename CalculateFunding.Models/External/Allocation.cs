namespace CalculateFunding.Models.External
{
    public class Allocation
    {
        public Allocation(FundingStream fundingStream, Period period, Provider provider, AllocationLine allocationLine,
            int allocationVersionNumber, string status, double allocationAmount, int allocationLearnerCount)
        {
            FundingStream = fundingStream;
            Period = period;
            Provider = provider;
            AllocationLine = allocationLine;
            AllocationVersionNumber = allocationVersionNumber;
            Status = status;
            AllocationAmount = allocationAmount;
            AllocationLearnerCount = allocationLearnerCount;
        }

        public FundingStream FundingStream { get; set; }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        public AllocationLine AllocationLine { get; set; }

        public int AllocationVersionNumber { get; set; }

        public string Status { get; set; }

        public double AllocationAmount { get; set; }

        public int AllocationLearnerCount { get; set; }
    }
}