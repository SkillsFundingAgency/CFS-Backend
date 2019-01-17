using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.Filtering
{
	[TestClass]
	public class FilterTests
	{
		private const string TestFilterNameForStringFormattedFilter = "allocationStatus";
		private const string TestFilterNameForIntegerFormattedFilter = "startYear";
		private const string TestOperator = "eq";

		[TestMethod]
		public void GetFilterQuery_GivenEmptyFilters_ShouldReturnEmptyString()
		{
			// arrange
			List<string> filters = new List<string>();
			var filter = new Filter(TestFilterNameForStringFormattedFilter, filters, false, TestOperator);

			// act
			string generatedFilterQuery = filter.BuildOrFilterQuery();

			// assert
			generatedFilterQuery
				.Should().BeNull();
		}

		[TestMethod]
		public void GetFilterQuery_GivenSingleFilterThatIsInStringFormat_ShouldReturnCorrectFormattingWithoutOrClause()
		{
			// arrange
			const string allocationStatusPublishedFilterStatusValue = "Published";
			List<string> filters = new List<string>()
			{
				allocationStatusPublishedFilterStatusValue
			};


			var filter = new Filter(TestFilterNameForStringFormattedFilter, filters, false, TestOperator);

			// act
			string generatedFilterQuery = filter.BuildOrFilterQuery();

			// assert
			generatedFilterQuery
				.Should().Be($"({TestFilterNameForStringFormattedFilter} {TestOperator} '{allocationStatusPublishedFilterStatusValue}')");
		}

		[TestMethod]
		public void GetFilterQuery_GivenSingleFilterThatIsInIntegerFormat_ShouldReturnCorrectQueryWithoutOrClause()
		{
			// arrange
			const string startYearFilter = "2018";
			List<string> filters = new List<string>
			{
				startYearFilter
			};
			
			var filter = new Filter(TestFilterNameForIntegerFormattedFilter, filters, true, TestOperator);

			// act
			string generatedFilterQuery = filter.BuildOrFilterQuery();

			// assert
			generatedFilterQuery
				.Should().Be($"({TestFilterNameForIntegerFormattedFilter} {TestOperator} {startYearFilter})");
		}

		[TestMethod]
		public void GetFilterQuery_GivenMultipleFiltersThatIsInIntegerFormat_ShouldReturnCorrectQueryWithClause()
		{
			// arrange
			const string startYearFilter1 = "2018";
			const string startYearFilter2 = "2017";

			List<string> filters = new List<string>
			{
				startYearFilter1,
				startYearFilter2
			};

			var filter = new Filter(TestFilterNameForIntegerFormattedFilter, filters, true, TestOperator);

			// act
			string generatedFilterQuery = filter.BuildOrFilterQuery();

			// assert
			generatedFilterQuery
				.Should().Be($"({TestFilterNameForIntegerFormattedFilter} {TestOperator} {startYearFilter1} or {TestFilterNameForIntegerFormattedFilter} {TestOperator} {startYearFilter2})");
		}

		[TestMethod]
		public void GetFilterQuery_GivenMultipleFiltersThatIsInStringFormat_ShouldReturnCorrectFormattingWithClause()
		{
			// arrange
			const string allocationStatusPublishedFilterStatusValue1 = "Published";
			const string allocationStatusPublishedFilterStatusValue2 = "Approved";
			List<string> filters = new List<string>()
			{
				allocationStatusPublishedFilterStatusValue1,
				allocationStatusPublishedFilterStatusValue2
			};


			var filter = new Filter(TestFilterNameForStringFormattedFilter, filters, false, TestOperator);

			// act
			string generatedFilterQuery = filter.BuildOrFilterQuery();

			// assert
			generatedFilterQuery
				.Should().Be($"({TestFilterNameForStringFormattedFilter} {TestOperator} '{allocationStatusPublishedFilterStatusValue1}' or {TestFilterNameForStringFormattedFilter} {TestOperator} '{allocationStatusPublishedFilterStatusValue2}')");
		}
	}
}
