using System;

namespace CalculateFunding.Api.External.V1.Models
{
    public class AllocationHistoryModel
    {
        public string Status { get; set; }

        public decimal? AllocationAmount { get; set; }

        public int AllocationVersionNumber { get; set; }

        public DateTimeOffset Date { get; set; }

        public string Comment { get; set; }

        public string Author { get; set; }

        public decimal AllocationVersion { get; set; }
    }
}
