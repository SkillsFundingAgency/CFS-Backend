using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    public class ExpectedProfileValue
    {
        public ProfilePeriodType Type { get; set; }

        public string TypeValue { get; set; }

        public int Year { get; set; }

        public int Occurrence { get; set; }

        public decimal ProfiledValue { get; set; }
    }
}