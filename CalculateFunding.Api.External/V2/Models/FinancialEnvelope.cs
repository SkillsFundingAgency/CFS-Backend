namespace CalculateFunding.Api.External.V2.Models
{
	public class FinancialEnvelope
	{
		public int MonthStart { get; set; }

		public int YearStart { get; set; }

		public int MonthEnd { get; set; }

		public int YearEnd { get; set; }

		public decimal Value { get; set; }
	}
}
