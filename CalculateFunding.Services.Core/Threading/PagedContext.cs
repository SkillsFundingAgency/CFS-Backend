using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CalculateFunding.Services.Core.Threading
{
    public class PagedContext<TItem>
    {
        private const int PageSize = 5;

        private static readonly TItem[] EmptyItems = new TItem[0];

        private readonly TItem[] _items;
        private volatile int _page;
        private readonly int _pageSize;

        public PagedContext(IEnumerable<TItem> items, int pageSize = PageSize)
        {
            _pageSize = pageSize;
            _items = items?.ToArray() ?? EmptyItems;
            _page = 0;
        }

        public TItem[] NextPage()
        {
            int skipCount = _page * _pageSize;

            Interlocked.Increment(ref _page);

            return _items.Skip(skipCount).Take(PageSize).ToArray();
        }

        public bool HasPages => _items.Length >= _page * PageSize;
    }    
}