using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Results.Filtering
{
	public class FilterHelper
	{
		public FilterHelper()
		{
			Filters = new List<Filter>();
		}

		public FilterHelper(IList<Filter> filters)
		{
			Filters = filters;
		}

		public IList<Filter> Filters { get; set; }

		public string BuildAndFilterQuery()
		{
			return Filters.IsNullOrEmpty()
				? null
				: string.Join(" and ", Filters.Select(f => f.BuildOrFilterQuery()));
		}
	}
}
