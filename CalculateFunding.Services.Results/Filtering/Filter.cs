using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Results.Filtering
{
	public class Filter
	{
		private static string _stringFormat = "{0} {1} \'{2}\'";
		private static string _integerFormat = "{0} {1} {2}";

		public Filter(string filterName, IEnumerable<string> filters, bool isInt, string @operator)
		{
			FilterName = filterName;
			Filters = filters;
			IsInt = isInt;
			Operator = @operator;
		}

		public string FilterName { get; set; }

		public IEnumerable<string> Filters { get; set; }

		public bool IsInt { get; set; }

		public string Operator { get; set; }

		public string BuildOrFilterQuery()
		{
			string filterFormat = IsInt ? _integerFormat : _stringFormat;

			return Filters.IsNullOrEmpty()
				? null
				: $"({string.Join(" or ", Filters.Select(f => string.Format(filterFormat, FilterName, Operator, f)))})";
		}
	}
}
