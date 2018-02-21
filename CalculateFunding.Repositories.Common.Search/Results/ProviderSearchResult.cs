using System;
using System.Collections.Generic;

namespace CalculateFunding.Repositories.Common.Search.Results
{
	public class ProviderSearchResult
	{
		public string UKPRN { get; set; }
		public string URN { get; set; }
		public string UPIN { get; set; }
		public string EstablishmentNumber { get; set; }
		public string Rid { get; set; }
		public string Name { get; set; }
		public string Authority { get; set; }
		public string ProviderType { get; set; }
		public string ProviderSubType { get; set; }
		public DateTimeOffset? OpenDate { get; set; }
		public DateTimeOffset? CloseDate { get; set; }
	}
}