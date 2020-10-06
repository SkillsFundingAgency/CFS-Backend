using System.Collections.Generic;
using CalculateFunding.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class SearchModelBuilder : TestEntityBuilder
    {
        private int _pageNumber;
        private int _top = 1;
        private bool _includeFacets;
        private string _searchTerm;
        private IDictionary<string, string[]> _filters;

        public SearchModelBuilder AddFilter(string name, params string[] filters)
        {
            _filters ??= new Dictionary<string, string[]>();
            _filters.Add(name, filters);

            return this;
        }

        public SearchModelBuilder WithSearchTerm(string searchTerm)
        {
            _searchTerm = searchTerm;

            return this;
        }
        
        
        public SearchModelBuilder WithIncludeFacets(bool includeFacets)
        {
            _includeFacets = includeFacets;

            return this;
        }

        public SearchModelBuilder WithPageNumber(int pageNumber)
        {
            _pageNumber = pageNumber;

            return this;
        }

        public SearchModelBuilder WithTop(int top)
        {
            _top = top;

            return this;
        }
        
        public SearchModel Build()
        {
            return new SearchModel
            {
                PageNumber = _pageNumber, 
                Top = _top,
                IncludeFacets = _includeFacets,
                SearchTerm = _searchTerm ?? NewRandomString(),
                Filters = _filters
            };
        }
    }
}