using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Aggregations
{
    public class StatusCounts
    {
        public int Approved { get; set; }

        public int Updated { get; set; }

        public int Draft { get; set; }

        public int Total
        {
            get
            {
                return Approved + Updated + Draft;
            }
        }
    }
}
