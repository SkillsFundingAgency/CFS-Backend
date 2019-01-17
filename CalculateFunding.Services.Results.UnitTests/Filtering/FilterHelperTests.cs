using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.Filtering
{
	[TestClass]
	public class FilterHelperTests
	{
		private const string TestFilterName = "allocationStatus";
		private const string TestOperator = "eq";

		[TestMethod]
		public void BuildAndFilterQuery_GivenNoFilters_ShouldReturnNull()
		{
			// arrange
			FilterHelper filterHelper = new FilterHelper(new List<Filter>());

			// act
			string generatedQuery = filterHelper.BuildAndFilterQuery();

			// assert
			generatedQuery.Should().BeNull();
		}

		[TestMethod]
		public void BuildAndFilterQuery_GivenASingleFilter_ShouldReturnCorrectQueryWithoutAndClause()
		{
			// arrange
			const string allocationStatusPublishedFilterStatusValue = "Published";

			List<Filter> filters = new List<Filter>
			{
				new Filter(TestFilterName, new List<string> {allocationStatusPublishedFilterStatusValue}, false, TestOperator)
			};
			

			FilterHelper filterHelper = new FilterHelper(filters);
			

			// act
			string generatedQuery = filterHelper.BuildAndFilterQuery();

			// assert
			generatedQuery
				.Should().Be($"({TestFilterName} {TestOperator} '{allocationStatusPublishedFilterStatusValue}')");
		}

		[TestMethod]
		public void BuildAndFilterQuery_GivenMultipleFilters_ShouldReturnCorrectQueryWithAndClause()
		{
			// arrange
			const string allocationStatusPublishedFilterStatusValue = "Published";

			const string startYearFilterName = "startYear";
			const string startYear = "2018";

			List<Filter> filters = new List<Filter>
			{
				new Filter(TestFilterName, new List<string> {allocationStatusPublishedFilterStatusValue}, false, TestOperator),
				new Filter(startYearFilterName, new List<string> {startYear}, true, TestOperator)
			};
			FilterHelper filterHelper = new FilterHelper(filters);

			// act
			string generatedQuery = filterHelper.BuildAndFilterQuery();

			// assert
			generatedQuery
				.Should().Be($"({TestFilterName} {TestOperator} '{allocationStatusPublishedFilterStatusValue}') and ({startYearFilterName} {TestOperator} {startYear})");
		}
	}
}
