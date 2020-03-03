using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class ProfilingVersion
    {
        public int Version { get; set; }

        public DateTimeOffset Date { get; set; }

        public ProfileTotal[] ProfileTotals { get; set; }
    }
}
