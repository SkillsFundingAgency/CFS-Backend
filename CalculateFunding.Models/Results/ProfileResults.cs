using System;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class ProfileResults
    {
        public string AllocationLineId { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Amount { get; set; }
    }
}