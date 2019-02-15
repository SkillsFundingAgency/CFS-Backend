using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search.Results
{
	public class DatasetVersionSearchResults
	{
		public DatasetVersionSearchResults()
		{
			Results = Enumerable.Empty<DatasetVersionSearchResult>();
		}

		public int TotalCount { get; set; }

		public IEnumerable<DatasetVersionSearchResult> Results { get; set; }

	}
}
