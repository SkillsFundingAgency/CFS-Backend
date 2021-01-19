using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CalculateFunding.Services.Core.Threading
{
    public class PagedContext<TItem>
    {
        private const int DefaultPageSize = 5;

        private static readonly TItem[] EmptyItems = new TItem[0];

        private  TItem[] _items;
        private volatile int _page;
        private int _pageSize;

        public PagedContext(IEnumerable<TItem> items, int pageSize = DefaultPageSize)
        {
            InitialiseItems(items, pageSize);
        }

        protected void InitialiseItems(IEnumerable<TItem> items,
            int pageSize)
        {
            _pageSize = pageSize;
            _items = items?.ToArray() ?? EmptyItems;
            _page = 0;
        }

        public TItem[] NextPage()
        {
            int skipCount = _page * _pageSize;

            Interlocked.Increment(ref _page);

            return _items.Skip(skipCount).Take(_pageSize).ToArray();
        }

        public bool HasPages => _items.Length > _page * _pageSize;
    }    
}