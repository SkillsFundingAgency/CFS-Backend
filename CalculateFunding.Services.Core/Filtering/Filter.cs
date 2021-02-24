using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Core.Filtering
{
	public class Filter
	{
		private static string _stringFormat = "{0} {1} \'{2}\'";
		private static string _nonStringFormat = "{0} {1} {2}";

		public Filter(string filterName, IEnumerable<string> filters, bool isNonString, string @operator)
		{
			FilterName = filterName;
			Filters = filters;
			IsNonString = isNonString;
			Operator = @operator;
		}

		public string FilterName { get; set; }

		public IEnumerable<string> Filters { get; set; }

		public bool IsNonString { get; set; }

		public string Operator { get; set; }

		public string BuildOrFilterQuery()
		{
			string filterFormat = IsNonString ? _nonStringFormat : _stringFormat;

			return Filters.IsNullOrEmpty()
				? null
				: $"({string.Join(" or ", Filters.Select(f => string.Format(filterFormat, FilterName, Operator, f)))})";
		}
	}
}
