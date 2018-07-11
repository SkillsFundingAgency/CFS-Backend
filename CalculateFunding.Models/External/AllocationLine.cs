using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.External
{
    public class AllocationLine
    {
        public string AllocationLineCode { get; set;}
        public string AllocationLineName { get; set; }

        public AllocationLine(string allocationLineCode, string allocationLineName)
        {
            AllocationLineCode = allocationLineCode;
            AllocationLineName = allocationLineName;
        }
    }
}
