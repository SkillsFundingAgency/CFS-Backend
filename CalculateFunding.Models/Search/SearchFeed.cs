using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Search
{
    public class SearchFeed<T> where T: class
    {
        public string Id
        {
            get
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        public int PageRef { get; set; }

        public int Top { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages
        {
            get
            {
                if (TotalCount <= Top)
                {
                    return 1;
                }

                double pages = ((double)TotalCount / (double)Top);

                return (int)Math.Ceiling(pages);
            }
        }

        public int Self
        {
            get
            {
                return PageRef;
            }
        }

        public int Last
        {
            get
            {
                return TotalPages;
            }
        }

        public int Next
        {
            get
            {
                if(Self >= TotalPages)
                {
                    return Self;
                }

                return Self + 1;
            }
        }

        public int Previous
        {
            get
            {
                if(Self == 1)
                {
                    return Self;
                }

                return Self - 1;
            }
        }

        public IEnumerable<T> Entries { get; set; }
    }
}
