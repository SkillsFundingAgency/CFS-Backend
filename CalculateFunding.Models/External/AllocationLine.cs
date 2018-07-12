namespace CalculateFunding.Models.External
{
    public class AllocationLine
    {
        public AllocationLine(string allocationLineCode, string allocationLineName)
        {
            AllocationLineCode = allocationLineCode;
            AllocationLineName = allocationLineName;
        }

        public string AllocationLineCode { get; set; }

        public string AllocationLineName { get; set; }
    }
}