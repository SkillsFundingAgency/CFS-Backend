using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models
{
    public class SearchModel
    {
        public int PageNumber { get; set; }

        public int Top { get; set; }

        public string SearchTerm { get; set; } = "*";
    }
}
