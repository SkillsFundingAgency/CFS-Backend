using System;

namespace CalculateFunding.Models.Results
{
	public class AllocationResult
	{
		public Reference AllocationLine { get; set; }
		public decimal? Value { get; set; }
		public Exception Exception { get; set; }

	}
}