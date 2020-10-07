using CalculateFunding.Common.Models;

namespace CalculateFunding.Services.Profiling.Models
{
    public class FinancialEnvelope
    {
        public Month MonthStart { get; set; }

        public int YearStart { get; set; }

        public Month MonthEnd { get; set; }

        public int YearEnd { get; set; }

        public decimal Value { get; set; }
    }
}
